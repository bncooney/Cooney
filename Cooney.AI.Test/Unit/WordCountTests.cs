using Cooney.AI.Tools;

namespace Cooney.AI.Test.Unit
{
	[TestClass]
	public class WordCountTests
	{
		[TestMethod]
		public void CountWords_Basic()
		{
			// Arrange
			string text = "Hello world!";

			// Act
			var (wordCount, sentenceCount, paragraphCount) = WordCount.CountWordsSentencesParagraphs(text);

			// Assert
			Assert.AreEqual(2, wordCount);
			Assert.AreEqual(1, sentenceCount);
			Assert.AreEqual(1, paragraphCount);
		}

		[TestMethod]
		public void CountWords_MultipleWordsAndSpaces()
		{
			string text = "   Hello   world   this   is   a   test   ";

			var (wordCount, sentenceCount, paragraphCount) = WordCount.CountWordsSentencesParagraphs(text);

			Assert.AreEqual(6, wordCount);
			Assert.AreEqual(0, sentenceCount);
			Assert.AreEqual(1, paragraphCount);
		}

		[TestMethod]
		public void CountSentences_WithMultipleSeparators()
		{
			string text = "Hello. How are you? I am fine.";

			var (wordCount, sentenceCount, paragraphCount) = WordCount.CountWordsSentencesParagraphs(text);

			Assert.AreEqual(7, wordCount);
			Assert.AreEqual(3, sentenceCount);
			Assert.AreEqual(1, paragraphCount);
		}

		[TestMethod]
		public void CountParagraphs_MultipleParagraphs()
		{
			string text = "First paragraph.\n\nSecond paragraph.\n\nThird paragraph.";

			var (wordCount, sentenceCount, paragraphCount) = WordCount.CountWordsSentencesParagraphs(text);

			Assert.AreEqual(6, wordCount);
			Assert.AreEqual(3, sentenceCount);
			Assert.AreEqual(3, paragraphCount);
		}
	}
}
