

using Babeltut.App.Domain.Entities;
using Babeltut.App.Domain.Interfaces;
using Babeltut.App.DTO;
using Babeltut.App.Orchestration;

using Moq;
using OneOf;
using static Babeltut.App.Extensions.OneOfExtensions;

namespace Babeltut.App.Tests.Orchestration;

public class FileDispatcherTests
{
    private readonly Mock<IWordTranslator> _wordTranslator = new();
    private readonly Mock<IExcelTranslator> _excelTranslator = new();
    private readonly Mock<IErrorHandler> _appError = new();
    private readonly FileDispatcher _dispatcher;

    private static readonly string TestDataPath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "TestData"
    );

    public FileDispatcherTests()
    {
        _dispatcher = new FileDispatcher(_wordTranslator.Object, _excelTranslator.Object, _appError.Object);
    }

    [Fact]
    public async Task Dispatch_FileNotFound_CallErrorHandler()
    {
        await _dispatcher.Dispatch(
            new UserInputsDTO(
                "fr", "en", 
                "inexistant.docx", 
                null, null)
         );

        _appError.Verify(e => e.Warns(It.Is<DocumentError>(de => de.ErrCode == ErrorCodes.FileNotFound)), Times.Once);
    }

    [Fact]
    public async Task Dispatch_ValidWord_CallWordTranslator()
    {
        var inputs = new UserInputsDTO(
            "fr", "en", 
            Path.Combine(TestDataPath, "Word_Valid_FR.docx"), 
            null, null
        );

        await _dispatcher.Dispatch(inputs);

        _wordTranslator.Verify(t => t.Translate(It.IsAny<WordDocument>()), Times.Once);
        _appError.Verify(e => e.Warns(It.IsAny<DocumentError>()), Times.Never);
    }


    [Fact]
    public async Task Dispatch_ValidOutputPath_CallWordTranslator()
    {
        var inputs = new UserInputsDTO(
            "fr", "en",
            Path.Combine(TestDataPath, "Word_Valid_FR.docx"),
            TestDataPath, null
        );

        await _dispatcher.Dispatch(inputs);

        _wordTranslator.Verify(t => t.Translate(It.IsAny<WordDocument>()), Times.Once);
        _appError.Verify(e => e.Warns(It.IsAny<DocumentError>()), Times.Never);
    }

    [Fact]
    public async Task Dispatch_WrongOutputPath_CallWordTranslator()
    {
        var inputs = new UserInputsDTO(
            "fr", "en",
            Path.Combine(TestDataPath, "Word_Valid_FR.docx"),
            "/pathThatDoesntExist/", null
        );

        await _dispatcher.Dispatch(inputs);

        _appError.Verify(e => e.Warns(It.Is<DocumentError>(de => de.ErrCode == ErrorCodes.OutputPathNotFound)), Times.Once);
        _wordTranslator.Verify(t => t.Translate(It.IsAny<WordDocument>()), Times.Never);
    }


    [Fact]
    public async Task Dispatch_ValidExcel_CallExcelTranslator()
    {

        var inputs = new UserInputsDTO(
            "fr", "en", 
            Path.Combine(TestDataPath, "Excel_Valid_FR.xlsx"), 
            null,
            new ExcelOptions (1,"A","B")
        );

        await _dispatcher.Dispatch(inputs);

        _excelTranslator.Verify(t => t.Translate(It.IsAny<ExcelDocument>()), Times.Once);
    }

    [Fact]
    public async Task Dispatch_FileNotSupported_CallErrorHandler()
    {
        var inputs = new UserInputsDTO(
            "fr", "en",
            Path.Combine(TestDataPath, "Invalid_TextFile.txt"), 
            null, null
        );

        await _dispatcher.Dispatch(inputs);

        _appError.Verify(e => e.Warns(It.Is<DocumentError>(de => de.ErrCode == ErrorCodes.FileNotSupported)), Times.Once);
        _wordTranslator.Verify(t => t.Translate(It.IsAny<WordDocument>()), Times.Never);
        _excelTranslator.Verify(t => t.Translate(It.IsAny<ExcelDocument>()), Times.Never);
    }

    [Fact]
    public async Task Dispatch_FileWithoutExt_CallErrorHandler()
    {
        var inputs = new UserInputsDTO(
            "fr", "en", Path.Combine(TestDataPath, "Invalid_File"), null, null
        );

        await _dispatcher.Dispatch(inputs);

        _appError.Verify(e => e.Warns(It.IsAny<DocumentError>()), Times.Once);
        _wordTranslator.Verify(t => t.Translate(It.IsAny<WordDocument>()), Times.Never);
        _excelTranslator.Verify(t => t.Translate(It.IsAny<ExcelDocument>()), Times.Never);
    }

    [Fact]
    public async Task Dispatch_InvalidColumns_CallErrorHandler()
    {
        var inputs = new UserInputsDTO(
            "fr", "en",
            Path.Combine(TestDataPath, "Excel_Valid_FR.xlsx"), null,
            new ExcelOptions(1, "AB", "AAAA")
        );

        await _dispatcher.Dispatch(inputs);

        _appError.Verify(e => e.Warns(It.Is<DocumentError>(de => de.ErrCode == ErrorCodes.InvalidColumnsFormat)), Times.Once);
        _excelTranslator.Verify(t => t.Translate(It.IsAny<ExcelDocument>()), Times.Never);
    }

    [Fact]
    public async Task Dispatch_InvalidColumns2_CallErrorHandler()
    {
        var inputs = new UserInputsDTO(
            "fr", "en",
            Path.Combine(TestDataPath, "Excel_Valid_FR.xlsx"), null,
            new ExcelOptions(1, "AB", "ZZZ")
        );

        await _dispatcher.Dispatch(inputs);

        _appError.Verify(e => e.Warns(It.Is<DocumentError>(de => de.ErrCode == ErrorCodes.InvalidColumnsFormat)), Times.Once);
        _excelTranslator.Verify(t => t.Translate(It.IsAny<ExcelDocument>()), Times.Never);
    }

    [Fact]
    public async Task Dispatch_SameColumns_CallErrorHandler()
    {
        var inputs = new UserInputsDTO(
            "fr", "en",
            Path.Combine(TestDataPath, "Excel_Valid_FR.xlsx"), null,
            new ExcelOptions(1, "A", "A")
        );

        await _dispatcher.Dispatch(inputs);

        _appError.Verify(e => e.Warns(It.Is<DocumentError>(de => de.ErrCode == ErrorCodes.SameColumns)), Times.Once);
        _excelTranslator.Verify(t => t.Translate(It.IsAny<ExcelDocument>()), Times.Never);
    }

    
}