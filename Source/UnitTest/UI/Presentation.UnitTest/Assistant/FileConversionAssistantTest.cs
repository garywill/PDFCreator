﻿using NSubstitute;
using NUnit.Framework;
using pdfforge.Obsidian;
using pdfforge.PDFCreator.Core.DirectConversion;
using pdfforge.PDFCreator.Core.Printing.Printing;
using pdfforge.PDFCreator.UI.Interactions;
using pdfforge.PDFCreator.UI.Interactions.Enums;
using pdfforge.PDFCreator.UI.Presentation.Assistants;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SystemInterface.IO;

namespace Presentation.UnitTest.Assistant
{
    [TestFixture]
    internal class FileConversionAssistantTest
    {
        [SetUp]
        public void Setup()
        {
            _directConversionHelper = Substitute.For<IDirectConversionHelper>();
            _printFileHelper = Substitute.For<IPrintFileHelper>();
            _fileWrap = Substitute.For<IFile>();
            _directoryWrap = Substitute.For<IDirectory>();
            _interactionInvoker = Substitute.For<IInteractionInvoker>();

            _fileConversionHandler = new FileConversionAssistant(_directConversionHelper, _printFileHelper, _fileWrap, _directoryWrap, _interactionInvoker, new UnitTestTranslationUpdater());

            _droppedFiles = new List<string> { "file1", "file2", "file3" };
        }

        private FileConversionAssistant _fileConversionHandler;
        private IDirectConversionHelper _directConversionHelper;
        private IPrintFileHelper _printFileHelper;
        private IFile _fileWrap;
        private List<string> _droppedFiles;
        private IDirectory _directoryWrap;
        private IInteractionInvoker _interactionInvoker;

        [Test]
        public void HandleDroppedFiles_Null_DoNothing()
        {
            _fileConversionHandler.HandleFileList(null);
            Assert.AreEqual(0, _directConversionHelper.ReceivedCalls().Count());
            Assert.AreEqual(0, _printFileHelper.ReceivedCalls().Count());
            Assert.AreEqual(0, _fileWrap.ReceivedCalls().Count());
        }

        [Test]
        public void HandleDroppedFiles_EmptyList_DoNothing()
        {
            var droppedFiles = new List<string>();
            _fileConversionHandler.HandleFileList(droppedFiles);
            Assert.AreEqual(0, _directConversionHelper.ReceivedCalls().Count());
            Assert.AreEqual(0, _printFileHelper.ReceivedCalls().Count());
            Assert.AreEqual(0, _fileWrap.ReceivedCalls().Count());
        }

        [Test]
        public void HandleDroppedFiles_ListWithExistingAndNonExistingPrintableFiles_ExistingFilesGetPrinted()
        {
            _fileWrap.Exists(_droppedFiles[0]).Returns(true);
            _fileWrap.Exists(_droppedFiles[1]).Returns(false);
            _fileWrap.Exists(_droppedFiles[2]).Returns(true);

            _directConversionHelper.CanConvertDirectly("").ReturnsForAnyArgs(false);
            _printFileHelper.AddFiles(null).ReturnsForAnyArgs(true);
            _fileConversionHandler.HandleFileList(_droppedFiles);
            Assert.AreEqual(2, _directConversionHelper.ReceivedCalls().Count());

            _printFileHelper.Received(1).AddFiles(Arg.Is<List<string>>(x => (x.Count == 2) && x[0].Equals(_droppedFiles[0]) && x[1].Equals(_droppedFiles[2])));
            _printFileHelper.Received(1).PrintAll();
        }

        [Test]
        public void HandleDroppedFiles_ListWithExistingAndNotExitstingDirectConversionFiles_ExistingFilesGetConverteDirectly()
        {
            _fileWrap.Exists(_droppedFiles[0]).Returns(true);
            _fileWrap.Exists(_droppedFiles[1]).Returns(false);
            _fileWrap.Exists(_droppedFiles[2]).Returns(true);
            _directConversionHelper.CanConvertDirectly("").ReturnsForAnyArgs(true);

            _fileConversionHandler.HandleFileList(_droppedFiles);

            Assert.AreEqual(0, _printFileHelper.ReceivedCalls().Count());
            _directConversionHelper.Received(1).CanConvertDirectly(_droppedFiles[0]);
            _directConversionHelper.Received(1).CanConvertDirectly(_droppedFiles[2]);
            _directConversionHelper.Received(1).ConvertDirectly(_droppedFiles[0]);
            _directConversionHelper.Received(1).ConvertDirectly(_droppedFiles[2]);
        }

        [Test]
        public void HandleDroppedFiles_ListWithExistingDirectConversionFiles_GetConverteDirectly()
        {
            _fileWrap.Exists("").ReturnsForAnyArgs(true);
            _directConversionHelper.CanConvertDirectly("").ReturnsForAnyArgs(true);

            _fileConversionHandler.HandleFileList(_droppedFiles);

            Assert.AreEqual(0, _printFileHelper.ReceivedCalls().Count());
            _directConversionHelper.Received(1).CanConvertDirectly(_droppedFiles[0]);
            _directConversionHelper.Received(1).CanConvertDirectly(_droppedFiles[1]);
            _directConversionHelper.Received(1).CanConvertDirectly(_droppedFiles[2]);
            _directConversionHelper.Received(1).ConvertDirectly(_droppedFiles[0]);
            _directConversionHelper.Received(1).ConvertDirectly(_droppedFiles[1]);
            _directConversionHelper.Received(1).ConvertDirectly(_droppedFiles[2]);
        }

        [Test]
        public void HandleDroppedFiles_ListWithExistingNonPrintableFiles_PrintFileHelperAddFilesAndPrintAllGetCalled()
        {
            _fileWrap.Exists("").ReturnsForAnyArgs(true);
            _directConversionHelper.CanConvertDirectly("").ReturnsForAnyArgs(false);
            _printFileHelper.AddFiles(null).ReturnsForAnyArgs(true);
            _fileConversionHandler.HandleFileList(_droppedFiles);
            Assert.AreEqual(3, _directConversionHelper.ReceivedCalls().Count());

            _printFileHelper.Received(1).AddFiles(Arg.Is<List<string>>(x => x.SequenceEqual(_droppedFiles)));
            _printFileHelper.Received(1).PrintAll();
        }

        [Test]
        public void HandleDroppedFiles_ListWithExistingPrintableFiles_PrintFileHelperAddFilesReturnsFalse_PrintAllDoeNotGetCalled()
        {
            _fileWrap.Exists("").ReturnsForAnyArgs(true);
            _directConversionHelper.CanConvertDirectly("").ReturnsForAnyArgs(false);
            _printFileHelper.AddFiles(null).ReturnsForAnyArgs(false);
            _fileConversionHandler.HandleFileList(_droppedFiles);
            Assert.AreEqual(3, _directConversionHelper.ReceivedCalls().Count());

            _printFileHelper.Received(1).AddFiles(Arg.Is<List<string>>(x => x.SequenceEqual(_droppedFiles)));
            _printFileHelper.DidNotReceive().PrintAll();
        }

        [Test]
        public void HandleDroppedFiles_ListWithNonExistingFiles_DoNothing()
        {
            _fileWrap.Exists("").ReturnsForAnyArgs(false);
            _fileConversionHandler.HandleFileList(_droppedFiles);
            Assert.AreEqual(0, _directConversionHelper.ReceivedCalls().Count());
            Assert.AreEqual(0, _printFileHelper.ReceivedCalls().Count());
        }

        [Test]
        public void HandleFileList_WithDirectory_AddsAllFilesFromDirectory()
        {
            var dir = @"X:\test";
            var file = Path.Combine(dir, "test.docx");

            _directoryWrap.Exists(dir).Returns(true);
            _directoryWrap.GetFiles(dir).Returns(new[] { file });

            _fileConversionHandler.HandleFileList(new[] { dir });

            _printFileHelper.Received(1).AddFiles(Arg.Is<IEnumerable<string>>(list => list.Contains(file)));
        }

        [Test]
        public void HandleFileList_WithDuplicates_AddsEachFileOnlyOnce()
        {
            var dir = @"X:\test";
            var file1 = Path.Combine(dir, "test.docx");
            var file2 = Path.Combine(dir, "test.docx");

            _directoryWrap.Exists(dir).Returns(true);
            _directoryWrap.GetFiles(dir).Returns(new[] { file1, file2 });

            _fileConversionHandler.HandleFileList(new[] { dir });

            _printFileHelper.Received(1).AddFiles(Arg.Is<IEnumerable<string>>(list => list.Count() == 1));
        }

        [Test]
        public void HandleFileList_ManyFiles_TriggersMessageInteraction()
        {
            _droppedFiles.Clear();

            _interactionInvoker.Invoke(Arg.Do<MessageInteraction>(i =>
                {
                    i.Response = MessageResponse.Yes;
                    var translation = new FileConversionAssistantTranslation();
                    Assert.AreEqual(translation.GetFormattedMoreThanXFilesQuestion(_droppedFiles.Count), i.Text);
                }
            ));

            for (int i = 0; i <= 100; i++)
            {
                _droppedFiles.Add("file" + i);
            }

            _fileWrap.Exists(Arg.Any<string>()).Returns(true);

            _fileConversionHandler.HandleFileList(_droppedFiles);

            _interactionInvoker.Received(1).Invoke(Arg.Any<MessageInteraction>());
            _printFileHelper.Received(1).AddFiles(Arg.Is<IEnumerable<string>>(list => list.Count() == _droppedFiles.Count));
        }

        [Test]
        public void HandleFileList_FewFiles_DoesNotTriggersMessageInteraction()
        {
            _fileWrap.Exists(Arg.Any<string>()).Returns(true);

            _fileConversionHandler.HandleFileList(_droppedFiles);

            _interactionInvoker.DidNotReceive().Invoke(Arg.Any<MessageInteraction>());
            _printFileHelper.Received(1).AddFiles(Arg.Is<IEnumerable<string>>(list => list.Count() == _droppedFiles.Count));
        }
    }
}