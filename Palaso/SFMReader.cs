using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Palaso
{
	public class SFMReader
	{
		private readonly StreamReader buffer;

		private enum State
		{
			Init,
			Tag,
			Text,
			Finished
		};

		private State _parseState = State.Init;
		private ParseMode _parseMode;

		public enum ParseMode
		{
			Default,
			Shoebox,
			Usfm
		}

		/// <summary>
		/// Construct a new SFMReader with filename
		/// </summary>
		/// <param name="fname"></param>
		public SFMReader(string fname)
		{
			buffer = new StreamReader(fname);
		}

		/// <summary>
		/// Construct a new SFMReader with stream
		/// </summary>
		/// <param name="stream"></param>
		public SFMReader(Stream stream)
		{
			buffer = new StreamReader(stream);
		}

		public ParseMode Mode
		{
			get { return _parseMode;  }
			set { _parseMode = value; }
		}

		/// <summary>
		/// Read next tag and return the name only (exclude backslash
		/// and space).
		/// </summary>
		/// <returns>next tag name</returns>
		public string ReadNextTag()
		{
			switch (_parseState)
			{
				case State.Init:
					ReadInitialText();
					break;
				case State.Text:
					ReadNextText();
					break;
			}
			if(_parseState == State.Finished)
			{
				return null;
			}
			Debug.Assert(_parseState == State.Tag);

			int c = buffer.Read(); // advance input stream over the initial \
			Debug.Assert(c == '\\' || c==-1);

			string tag ;
			bool hasReadNextChar = false;
			if(Mode == ParseMode.Usfm)
			{
				tag = GetNextToken(delegate(char ch) { return Char.IsWhiteSpace(ch) || ch == '\\' || ch == '*'; });
				if (buffer.Peek() == '*')
				{
					tag += (char) buffer.Read();
					hasReadNextChar = true;
				}
			}
			else
			{
				tag = GetNextToken(delegate(char ch) { return Char.IsWhiteSpace(ch) || ch == '\\'; });
			}

			if (tag == null)
			{
				_parseState = State.Finished;
				return null;
			}
			if (buffer.Peek() != '\\' && !hasReadNextChar)
			{
				c = buffer.Read(); // advance input stream over the terminating whitespace
				Debug.Assert(c == -1 || char.IsWhiteSpace((char) c));
			}
			_parseState = State.Text;
			return tag;
	   }

		private string GetNextToken(Predicate<char> isTokenTerminator)
		{
			string token = string.Empty;
			for(;;)
			{
				int peekedChar = buffer.Peek();
				if (peekedChar == -1)//end of stream
				{
					if (token.Length == 0)
						token = null;
					break;
				}

				if (isTokenTerminator((char) peekedChar))
					break;

				token += (char) buffer.Read();
			}
			return token;
		}

		/// <summary>
		/// Read next text block from stream
		/// </summary>
		/// <returns>Next text</returns>
		public string ReadNextText()
		{
			if (_parseState == State.Init)
			{
				ReadInitialText();
			}
			if(_parseState == State.Tag)
			{
				ReadNextTag();
			}
			return ReadText();
		}

		//public LinkedList<string> Tokenize(string txt)
		//{
		//    LinkedList<string> rtn = new LinkedList<string>();
		//    Regex reWords = new Regex(@"\s*(\S+)");
		//    Match m = reWords.Match(txt);
		//    if(m.Success)
		//    {
		//        rtn.AddLast(m.Groups[1].Value);
		//    }
		//    return rtn;
		//}

		public string ReadInitialText()
		{
			if(_parseState != State.Init)
				throw new InvalidOperationException("ReadInitialText must be called before ReadNextText or ReadNextTag");

			return ReadText() ?? "";
		}

		private string ReadText()
		{
			if (_parseState == State.Finished)
				return null;
			string text=string.Empty;
			do
			{
				string token = GetNextToken(delegate(char c) { return c == '\\'; });
				text += token;
				if (Mode == ParseMode.Shoebox)
				{
					if (token == null)
						break;
					if (text.Length > 0 && text[text.Length - 1] != '\n' && buffer.Peek() != -1)
					{
						text += (char)buffer.Read();
					}
					else break;
				}
			} while (Mode == ParseMode.Shoebox);
			if (text == null)
			{
				_parseState = State.Finished;
				return "";
			}
			_parseState = State.Tag;
			return text;
		}
	}
}
