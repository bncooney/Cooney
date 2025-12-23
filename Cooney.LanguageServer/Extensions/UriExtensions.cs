using System;
using System.IO;

namespace Cooney.LanguageServer.Extensions;

/// <summary>
/// Extension methods for working with URIs in the LSP context.
/// </summary>
public static class UriExtensions
{
	/// <summary>
	/// Converts a file path to a file URI string.
	/// </summary>
	/// <param name="filePath">The file path to convert.</param>
	/// <returns>A file URI string (e.g., "file:///C:/path/to/file.txt").</returns>
	public static string ToFileUri(string filePath)
	{
		if (string.IsNullOrWhiteSpace(filePath))
			throw new ArgumentException("File path cannot be null or whitespace.", nameof(filePath));

		// Make the path absolute if it's not already
		var absolutePath = Path.GetFullPath(filePath);

		// Create URI from absolute path
		var uri = new Uri(absolutePath, UriKind.Absolute);

		return uri.AbsoluteUri;
	}

	/// <summary>
	/// Converts a file URI to a local file path.
	/// </summary>
	/// <param name="fileUri">The file URI to convert.</param>
	/// <returns>A local file path.</returns>
	public static string ToFilePath(string fileUri)
	{
		if (string.IsNullOrWhiteSpace(fileUri))
			throw new ArgumentException("File URI cannot be null or whitespace.", nameof(fileUri));

		if (!Uri.TryCreate(fileUri, UriKind.Absolute, out var uri))
			throw new ArgumentException($"Invalid URI: {fileUri}", nameof(fileUri));

		if (!uri.IsFile)
			throw new ArgumentException($"URI is not a file URI: {fileUri}", nameof(fileUri));

		return uri.LocalPath;
	}

	/// <summary>
	/// Converts a file URI to a local file path.
	/// </summary>
	/// <param name="uri">The URI to convert.</param>
	/// <returns>A local file path.</returns>
	public static string ToFilePath(this Uri uri)
	{
		if (uri == null)
			throw new ArgumentNullException(nameof(uri));

		if (!uri.IsFile)
			throw new ArgumentException($"URI is not a file URI: {uri}", nameof(uri));

		return uri.LocalPath;
	}
}
