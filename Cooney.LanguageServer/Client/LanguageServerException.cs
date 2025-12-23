using System;

namespace Cooney.LanguageServer.Client;

/// <summary>
/// Exception thrown when a language server operation fails.
/// </summary>
public class LanguageServerException : Exception
{
	/// <summary>
	/// Gets the LSP error code, if available.
	/// </summary>
	public int? ErrorCode { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="LanguageServerException"/> class.
	/// </summary>
	/// <param name="message">The error message.</param>
	public LanguageServerException(string message)
		: base(message)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="LanguageServerException"/> class.
	/// </summary>
	/// <param name="message">The error message.</param>
	/// <param name="errorCode">The LSP error code.</param>
	public LanguageServerException(string message, int errorCode)
		: base(message)
	{
		ErrorCode = errorCode;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="LanguageServerException"/> class.
	/// </summary>
	/// <param name="message">The error message.</param>
	/// <param name="innerException">The inner exception.</param>
	public LanguageServerException(string message, Exception innerException)
		: base(message, innerException)
	{
	}
}
