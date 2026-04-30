using System.Diagnostics;

using OneOf;

using Babeltut.App.Domain.Entities;
using Babeltut.App.Domain.ValueObjects;
using Babeltut.App.DTO;
using Babeltut.App.Domain.Interfaces;

using static Babeltut.App.Extensions.OneOfExtensions;

namespace Babeltut.App.Orchestration;

//gere les erreurs liés au routing

/// <summary>
/// Sort and send the file to the right service for translation
/// </summary>
public class FileDispatcher(IWordTranslator wordService, IExcelTranslator excelService, IErrorHandler appError)
{

    /// <summary>
    /// Tries to send the infos to translations based on the user inputs
    /// </summary>
    /// <param name="inputs"></param>
    /// <returns></returns>
    public async Task Dispatch(UserInputsDTO inputs)
    {
        var err = ValidateFilePath(inputs) ?? ValidateOutputDirPath(inputs);
        if (err is DocumentError)
        {
            await SendToError(err); return;
        }

        var file = CreateDocument(inputs);
        await file.Match(
            word => wordService.Translate(word),
            excel => excelService.Translate(excel),
            error => SendToError(error)
        );
    }

    /// <summary>
    /// Consumme any async before sending sync to AppError Service
    /// </summary>
    /// <param name="err"></param>
    /// <returns></returns>
    private Task SendToError(DocumentError err)
    {
        appError.Warns(err);
        return Task.CompletedTask;
    }

    private OneOf<WordDocument, ExcelDocument, DocumentError> CreateDocument(UserInputsDTO inputs)
    {
        var extension = Path.GetExtension(inputs.FilePath).ToLower();

        return extension switch
        {
            ".xlsx" => TryCreateExcelDocument(inputs).AsDispatcherResult(),
            ".doc" or ".docx" => CreateWordDocument(inputs),
            _ => new DocumentError(inputs, ErrorCodes.FileNotSupported)
        };
    }
    private OneOf<ExcelDocument, DocumentError> TryCreateExcelDocument(UserInputsDTO inputs)
    {
        var translationStyleResult = ResolveExcelStrategy(inputs);
        if (translationStyleResult.TryPickT2(out var error, out var translationStyle))
            return error;

        return CreateExcelDocument(inputs, translationStyle);
    }
    private WordDocument CreateWordDocument(UserInputsDTO inputs)
    {
        return new WordDocument(
                                    inputs.From,
                                    inputs.To,
                                    inputs.FilePath,
                                    true,
                                    CreateOutputStrategy(inputs.OutputPath)
               );
    }

    private ExcelDocument CreateExcelDocument(UserInputsDTO inputs, OneOf<WholeSheet, Columns> translationStrat)
    {
        return new ExcelDocument(
                                    inputs.From,
                                    inputs.To,
                                    inputs.FilePath,
                                    false,
                                    CreateOutputStrategy(inputs.OutputPath),
                                    translationStrat
                );
    }

    /// <summary>
    /// Valide et renvoie la strategie d'output dir
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    private OneOf<SameDirectory, OutputPathDirectory> CreateOutputStrategy(string? path) => path switch
    {
        null => new SameDirectory(),
        _ => new OutputPathDirectory(path)
    };

    /// <summary>
    /// Basic check of the length of columns names in Excel
    /// </summary>
    /// <param name="column"></param>
    /// <returns></returns>
    private bool IsValidExcelColumnFormat(string column)
    {
        return  column.Length > 0 && //min A
                column.Length <= 3 && //max chars
                String.CompareOrdinal(column, "XFD") <= 0; // max col value "XFD"
    }

    /// <summary>
    /// Valide et renvoie le style de traduction à appliquer sur un excel ou une erreur si un param est incorrect
    /// </summary>
    /// <param name="inputs"></param>
    /// <returns></returns>
    private OneOf<WholeSheet, Columns, DocumentError> ResolveExcelStrategy(UserInputsDTO inputs)
    {
        Debug.Assert(inputs.Options is not null, "ExcelOptionsDTO should never be null here");
        if (inputs.Options is null) throw new InvalidOperationException("The excel options object is null");

        var columnFrom = inputs.Options.ColumnFrom;
        var columnTo = inputs.Options.ColumnTo;
        var sheet = inputs.Options.SheetNumber;

        //volontaire, l'user a rien mis
        if (string.IsNullOrWhiteSpace(columnFrom) && string.IsNullOrWhiteSpace(columnTo))
            return new WholeSheet(sheet);

        //format invalide
        if (!IsValidExcelColumnFormat(columnFrom!) || !IsValidExcelColumnFormat(columnTo!))
            return new DocumentError(inputs, ErrorCodes.InvalidColumnsFormat);

        //format valide mais 2x la meme col
        if (columnFrom == columnTo)
            return new DocumentError(inputs, ErrorCodes.SameColumns);

        return new Columns(columnFrom!, columnTo!, sheet);
    }

    /// <summary>
    /// Checks the File path of the user inputs
    /// </summary>
    /// <param name="inputs"></param>
    /// <returns>A DocumentError with details if failed, null otherwise</returns>
    private DocumentError? ValidateFilePath(UserInputsDTO inputs)
    {
        if (!File.Exists(inputs.FilePath)) return new DocumentError(inputs, ErrorCodes.FileNotFound);

        return null;
    }
    /// <summary>
    /// Checks the Output Directory path if not null.
    /// </summary>
    /// <param name="inputs"></param>
    /// <returns></returns>
    private DocumentError? ValidateOutputDirPath(UserInputsDTO inputs)
    {
        if (inputs.OutputPath is not null)
            if (!Directory.Exists(inputs.OutputPath))
                return new DocumentError(inputs, ErrorCodes.OutputPathNotFound);

        return null;
    }

}