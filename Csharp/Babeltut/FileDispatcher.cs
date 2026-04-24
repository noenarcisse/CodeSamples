
using Babeltut.App.Domain.Entities;
using Babeltut.App.Domain.ValueObjects;
using Babeltut.App.DTO;
using OneOf;
using System.Diagnostics;
using static Babeltut.App.Extensions.OneOfExtensions;

namespace Babeltut.App.Orchestration;

/// <summary>
/// Sort and send the file to the right service for translation
/// </summary>
public static class FileDispatcher
{
    /// <summary>
    /// Tries to send the infos to translations based on the user inputs
    /// </summary>
    /// <param name="inputs"></param>
    /// <returns></returns>
    public async static Task Dispatch(UserInputsDTO inputs)
    {
        var err = ValidateFilePath(inputs) ?? ValidateOutputDirPath(inputs);
        if (err is DocumentError)
        {
            await SendToError(err); return;
        }

        var file = CreateDocument(inputs);
        await file.Match(
            word => DocXTranslator.Translate(word),
            excel => XLTranslatorService.Translate(excel),
            error => SendToError(error)
        );
    }

    private static Task SendToError(DocumentError err)
    {
        AppError.Warns(err);
        return Task.CompletedTask;
    }

    private static OneOf<WordDocument, ExcelDocument, DocumentError> CreateDocument(UserInputsDTO inputs)
    {
        var extension = Path.GetExtension(inputs.FilePath).ToLower();

        return extension switch
        {
            ".xlsx" => TryCreateExcelDocument(inputs).AsDispatcherResult(),
            ".doc" or ".docx" => CreateWordDocument(inputs),
            _ => new DocumentError(inputs, "This file type is not supported")
        };
    }
    private static OneOf<ExcelDocument, DocumentError> TryCreateExcelDocument(UserInputsDTO inputs)
    {
        var translationStyleResult = ResolveExcelStrategy(inputs);
        if (translationStyleResult.TryPickT2(out var error, out var translationStyle))
            return error;

        return CreateExcelDocument(inputs, translationStyle);
    }
    private static WordDocument CreateWordDocument(UserInputsDTO inputs)
    {
        return new WordDocument(
                                    inputs.From,
                                    inputs.To,
                                    inputs.FilePath,
                                    true,
                                    CreateOutputStrategy(inputs.OutputPath)
               );
    }

    private static ExcelDocument CreateExcelDocument(UserInputsDTO inputs, OneOf<WholeSheet, Columns> translationStrat)
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
    private static OneOf<SameDirectory, OutputPathDirectory> CreateOutputStrategy(string? path) => path switch
    {
        null => new SameDirectory(),
        _ => new OutputPathDirectory(path)
    };

    /// <summary>
    /// Basic check of the length of columns names in Excel
    /// </summary>
    /// <param name="column"></param>
    /// <returns></returns>
    private static bool IsValidExcelColumnFormat(string column)
    {
        return  column.Length > 0 && //min A
                column.Length <= 3; //max value XFD
    }

    /// <summary>
    /// Valide et renvoie le style de traduction à appliquer sur un excel ou une erreur si un param est incorrect
    /// </summary>
    /// <param name="inputs"></param>
    /// <returns></returns>
    private static OneOf<WholeSheet, Columns, DocumentError> ResolveExcelStrategy(UserInputsDTO inputs)
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
            return new DocumentError(inputs, "There is an error with the columns format for an excel document");

        //format valide mais 2x la meme col
        if (columnFrom == columnTo)
            return new DocumentError(inputs, "The excel columns cannot be the same");

        return new Columns(columnFrom!, columnTo!, sheet);
    }

    /// <summary>
    /// Checks the File path of the user inputs
    /// </summary>
    /// <param name="inputs"></param>
    /// <returns>A DocumentError with details if failed, null otherwise</returns>
    private static DocumentError? ValidateFilePath(UserInputsDTO inputs)
    {
        if (!File.Exists(inputs.FilePath)) return new DocumentError(inputs, "The file cannot be found");

        return null;
    }
    /// <summary>
    /// Checks the Output Directory path if not null.
    /// </summary>
    /// <param name="inputs"></param>
    /// <returns></returns>
    private static DocumentError? ValidateOutputDirPath(UserInputsDTO inputs)
    {
        if (inputs.OutputPath is not null)
            if (!Directory.Exists(inputs.OutputPath))
                return new DocumentError(inputs, "The output directory does not exist");

        return null;
    }

}