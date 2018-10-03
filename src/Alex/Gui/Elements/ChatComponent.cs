﻿using System;
using System.Collections.Generic;
using System.Linq;
using Alex.API.Data;
using Alex.API.Graphics.Typography;
using Alex.API.Gui;
using Alex.API.Gui.Elements.Controls;
using Alex.API.Gui.Graphics;
using Alex.API.Input;
using Alex.API.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using NLog;

namespace Alex.Gui.Elements
{
	public class ChatComponent : GuiTextInput, IChatReceiver
	{
		private static readonly Logger Log = LogManager.GetCurrentClassLogger(typeof(ChatComponent));

		private object _lock = new object();
		private SortedList<DateTime, ChatObject> _chatEntries =
			new SortedList<DateTime, ChatObject>(new DescendedDateComparer());

		public IChatProvider ChatProvider;

	//	private TextInputBuilder _textBuilder;

		public ChatComponent()
		{
			Anchor = Alignment.BottomLeft;

			MaxHeight = Height;
			Height = 180;
			Width = 320;
		}

		private IFont Font;

		protected override void OnInit(IGuiRenderer renderer)
		{
			Font = renderer.Font;

			FocusOutlineThickness = Thickness.Zero;
			HighlightOutlineThickness = Thickness.Zero;

			BackgroundOverlay = Color.Transparent;
			FocusOutlineColor = Color.Transparent;
			HighlightOutlineColor = Color.Transparent;

			base.TextElement.Anchor = Alignment.BottomLeft;

			BorderColor = Color.Transparent;

		}

		protected override void OnFocusActivate()
		{
			//base.OnFocusActivate();
			Alex.Instance.IsMouseVisible = true;
			TextBuilder.Clear();
			Value = string.Empty;
			
			ResetTabComplete();

			TextElement.Text = string.Empty;
			Dismiss();
		}

		protected override void OnFocusDeactivate()
		{
		//	base.OnFocusDeactivate();
			Alex.Instance.IsMouseVisible = false;
			TextBuilder.Clear();
			Value = string.Empty;
			ResetTabComplete();

			TextElement.Text = string.Empty;
			Dismiss();
		}

		protected override void OnUpdate(GameTime gameTime)
		{
			/*if (Focused)
			{
				var msg = Value;
				var preCursor = msg.Substring(0, _textBuilder.CursorPosition);
				var cursorOffsetX = (int) Font.MeasureString(preCursor, Scale).X;
				_cursorPositionX = cursorOffsetX;

				var delta = (float) gameTime.TotalGameTime.TotalMilliseconds / 2;
				_cursorAlpha = (float) MathHelpers.SinInterpolation(0.1f, 0.5f, delta) * 2;
			}
			*/
			base.OnUpdate(gameTime);
		}

		//	private ChatObject[] Rendered

		private int _scrollValue = 0;
		private TimeSpan _renderTimeout = TimeSpan.FromSeconds(30);

		//private int _renderedC
		protected override void OnDraw(GuiSpriteBatch graphics, GameTime gameTime)
		{
			base.OnDraw(graphics, gameTime);

			if (Focused)
			{
				var renderPos = (TextElement.RenderBounds.BottomLeft() - new Vector2(0, 8)).ToPoint();
				graphics.FillRectangle(new Rectangle(RenderBounds.X, (renderPos.Y) - 2, Width, 10), new Color(Color.Black, 0.5f));
			}

			KeyValuePair<DateTime, ChatObject>[] entries;
			lock (_lock)
			{
				entries = _chatEntries.ToArray();
			}

			if (entries.Length > 0)
			{
				int renderedCount = 0;
				int skipped = 0;
				DateTime now = DateTime.UtcNow;
				Vector2 offset = new Vector2(0, -33);
				foreach (var msg in entries)
				{
					var elapse = now - msg.Key;
					float alpha = 1f;
					if (!Focused)
					{
						if (elapse > _renderTimeout)
						{
							continue;
						}

						alpha = (float) (1f - ((elapse.TotalMilliseconds / _renderTimeout.TotalMilliseconds) * 1f));
					}
					else
					{
						if (_scrollValue > 0)
						{
							if (skipped < _scrollValue)
							{
								skipped++;
								continue;
							}
						}
					}

					string message = msg.Value.RawMessage;
					foreach (var line in CalculateLines(message))
					{
						DrawChatLine(graphics, line, alpha, ref offset);
						renderedCount++;

						if (renderedCount >= 10) break;
					}

					//	if (!Focused)
					{
						if (renderedCount >= 10) break;
					}
				}
			}
		}

		private string GetFitting(string text, out string rest)
		{
			rest = string.Empty;

			var size = Font.MeasureString(text);
			while (size.X > Bounds.Width)
			{
				string current = text;
				text = current.Remove(current.Length - 1, 1);
				rest = current.Substring(current.Length - 1, 1) + rest;

				size = Font.MeasureString(text);
			}

			return text;
		}

		private string[] CalculateLines(string text)
		{
			var size = Font.MeasureString(text);
			if (size.X > Bounds.Width)
			{
				List<string> output = new List<string>();
				do
				{
					string result = GetFitting(text, out text);
					if (result.Length == 0) break;
					output.Add(result);

				} while (text.Length > 0);

				output.Reverse();

				return output.ToArray();
			}
			else
			{
				return new[] {text};
			}
		}

		private void DrawChatLine(GuiSpriteBatch graphics, string text, float alpha, ref Vector2 offset)
		{
			var size = Font.MeasureString(text);

			var renderPos = Bounds.BottomLeft() + offset;

			graphics.FillRectangle(new Rectangle(renderPos.ToPoint(), new Point(Width, (int) Math.Ceiling(size.Y + 2))),
				new Color(Color.Black, alpha * 0.5f));

			Font.DrawString(graphics.SpriteBatch, text, renderPos + new Vector2(0, 2), TextColor.White, opacity: alpha);
			offset.Y -= (size.Y + 2);
		}

		private int _latestTransactionId = -1;
		private int _tabCompletePosition = 0;
		protected override bool OnKeyInput(char character, Keys key)
		{
			if (Focused)
			{
				/*if (key == Keys.Back)
				{
					_textBuilder.RemoveCharacter();
					ResetTabComplete();
				}
				else if (key == Keys.Enter)
				{
					SubmitMessage();
					ResetTabComplete();
				}
				else if (key == Keys.Delete)
				{
					if (_textBuilder.CursorPosition < _textBuilder.Length)
					{
						_textBuilder.CursorPosition++;
						_textBuilder.RemoveCharacter();

						ResetTabComplete();
					}
				}
				else if (key == Keys.Tab)
				{
					if (_hasTabCompleteResults)
					{
						DoTabComplete(true);
						_prevWasTab = true;
						return true;
					}

					if (_textBuilder.Length == 0) return true;

					_textBuilder.CursorPosition = 1;
					string text = _textBuilder.GetAllBehindCursor(out _tabCompletePosition);
					if (text.StartsWith('/'))
					{
						_tabCompletePosition += 1;
						text = text.Substring(1, text.Length - 1);
					}

					ChatProvider?.RequestTabComplete(text, out _latestTransactionId);
					return true;
				}
				else
				{
					ResetTabComplete();
					if (Font.Characters.Any(x => x == character)) //Make sure it is a renderable character.
					{
						_textBuilder.AppendCharacter(character);
					}
				}*/

				if (key == Keys.Tab)
				{
					if (_hasTabCompleteResults)
					{
						DoTabComplete(true);
						_prevWasTab = true;
						return true;
					}

					if (TextBuilder.Length == 0) return true;

					TextBuilder.CursorPosition = 1;
					string text = TextBuilder.GetAllBehindCursor(out _tabCompletePosition);
					if (text.StartsWith('/'))
					{
						_tabCompletePosition += 1;
						text = text.Substring(1, text.Length - 1);
					}

					ChatProvider?.RequestTabComplete(text, out _latestTransactionId);
					return true;
				}
				else if (key == Keys.Enter)
				{
					SubmitMessage();
					ResetTabComplete();
				}
				else
				{
					int prevLength = TextBuilder.Length;
					base.OnKeyInput(character, key);
					if (TextBuilder.Length != prevLength)
					{
						ResetTabComplete();
					}
				}
				return true;
			}

			return false;
		}

		private void ResetTabComplete()
		{
			if (_hasTabCompleteResults)
			{
				_hasTabCompleteResults = false;
				_tabCompletePrevLength = 0;
			}
		}

		private bool _prevWasTab = false;
		private int _tabCompleteMatchIndex = 0;
		private int _tabCompletePrevLength = 0;
		private void DoTabComplete(bool incremental)
		{
			if (_hasTabCompleteResults)
			{
				string firstMatch = _tabCompleteMatches[_tabCompleteMatchIndex].Match;
				//_textBuilder.CursorPosition = _tabCompletePosition + _tabCompleteStart + _tabCompleteLength;

				if (_tabCompletePrevLength > 0)
				{
					TextBuilder.CursorPosition = _tabCompletePosition + _tabCompleteStart + _tabCompletePrevLength;
					for (int i = 0; i < _tabCompletePrevLength; i++)
					{
						TextBuilder.RemoveCharacter();
					}
				}
				else
				{
					TextBuilder.CursorPosition = _tabCompletePosition + _tabCompleteStart + _tabCompleteLength;
					for (int i = 0; i < _tabCompleteLength; i++)
					{
						TextBuilder.RemoveCharacter();
					}
				}

				TextBuilder.CursorPosition = _tabCompletePosition + _tabCompleteStart;
				TextBuilder.AppendLine(firstMatch);
				_tabCompletePrevLength = firstMatch.Length;

				if (incremental)
				{
					if (_tabCompleteMatchIndex + 1 >= _tabCompleteMatches.Length)
					{
						_tabCompleteMatchIndex = 0;
					}
					else
					{
						_tabCompleteMatchIndex++;
					}
				}
				else
				{
					if (_tabCompleteMatchIndex - 1 < 0)
					{
						_tabCompleteMatchIndex = _tabCompleteMatches.Length - 1;
					}
					else
					{
						_tabCompleteMatchIndex--;
					}
				}

				return;
			}
		}

		public void MoveCursor(bool add)
		{
			if (add)
			{
				TextBuilder.CursorPosition += 1;
			}
			else
			{
				TextBuilder.CursorPosition -= 1;
			}
		}

		public void ScrollDown()
		{
			if (!Focused) return;

			if (_scrollValue > 0)
			{
				_scrollValue--;
			}
		}

		public void ScrollUp()
		{
			if (!Focused) return;

			lock (_lock)
			{
				if (_scrollValue < _chatEntries.Count && _chatEntries.Count > 10)
				{
					_scrollValue++;
				}
			}
		}

		public void Dismiss()
		{
			//Enabled = false;
			//Focused = false;
			_scrollValue = 0;

			TextBuilder.Clear();
			Value = string.Empty;

			TextElement.Text = string.Empty;
		}

		private void SubmitMessage()
		{
			//Submit message
			if (TextBuilder.Length > 0)
			{
				if (Alex.IsMultiplayer)
				{
					ChatProvider?.Send(TextBuilder.Text);
				}
				else
				{
					Receive(new ChatObject(TextBuilder.Text));
				}
			}

			Dismiss();
		}

		public void Receive(ChatObject message)
		{
			lock (_lock)
			{
				_chatEntries.Add(DateTime.UtcNow, message);
			}
		}

		private int _tabCompleteStart, _tabCompleteLength;
		private TabCompleteMatch[] _tabCompleteMatches;
		private bool _hasTabCompleteResults = false;
		public void ReceivedTabComplete(int transactionId, int start, int length, TabCompleteMatch[] matches)
		{
			if (_latestTransactionId == transactionId)
			{
				if (matches.Length == 0) return;

				_tabCompletePrevLength = 0;
				_tabCompleteMatchIndex = 0;
				_tabCompleteMatches = matches;
				_tabCompleteStart = start;
				_tabCompleteLength = length;

				
				_hasTabCompleteResults = true;
				DoTabComplete(true);
			}
		}

		private class DescendedDateComparer : IComparer<DateTime>
		{
			public int Compare(DateTime x, DateTime y)
			{
				// use the default comparer to do the original comparison for datetimes
				int ascendingResult = Comparer<DateTime>.Default.Compare(x, y);

				// turn the result around
				return 0 - ascendingResult;
			}
		}
	}
}
