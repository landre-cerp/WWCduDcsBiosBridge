// Copyright © 2025 onwards, Andrew Whewell
// All rights reserved.
//
// Redistribution and use of this software in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
//    * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
//    * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
//    * Neither the name of the author nor the names of the program's contributors may be used to endorse or promote products derived from this software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE AUTHORS OF THE SOFTWARE BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

using McduDotNet;
using System.Text;

namespace WWCduDcsBiosBridge;


public class Scratchpad
{
    public const int HoldClrToEraseLineMilliseconds = 1000;
    public const int TogglePlusMinusMilliseconds = 1200;

    protected DateTime _ClrPressedUtc;
    protected DateTime _LastCharacterAdded;
    protected int _Column;
    protected Row _DataEntryRow;
    protected Row _MessageRow;
    protected bool _ShowingMessage;

    protected Cell CurrentCell => _DataEntryRow.Cells[
        Math.Max(
            0,
            Math.Min(_DataEntryRow.Cells.Length - 1, _Column)
        )
    ];

    public Row Row => _ShowingMessage ? _MessageRow : _DataEntryRow;

    public bool ShowingMessage => _ShowingMessage;

    public ScratchpadPlusMinusBehaviour PlusMinusBehaviour { get; set; }

    public bool ShowLowercaseInSmallUppercase { get; set; } = true;

    public string Text => GetDataEntryText();

    public bool OverflyTogglesCase { get; set; } = true;

    public bool IsLowerCase { get; set; }

    public event EventHandler RefreshRowDisplay;

    protected virtual void OnRefreshRowDisplay()
    {
        RefreshRowDisplay?.Invoke(this, EventArgs.Empty);
    }

    public Scratchpad()
    {
        _DataEntryRow = new Row();
        _MessageRow = new Row();
    }

    public virtual void KeyDown(Key key)
    {
        switch (key)
        {
            case Key.Clr:
                if (_ShowingMessage)
                {
                    _ClrPressedUtc = default;
                    HideMessage();
                }
                else
                {
                    _ClrPressedUtc = DateTime.UtcNow;
                    Backspace();
                }
                break;
            case Key.PositiveNegative:
                if (!_ShowingMessage)
                {
                    AddPlusMinus();
                }
                break;
            case Key.Ovfy:
                if (!_ShowingMessage && OverflyTogglesCase)
                {
                    IsLowerCase = !IsLowerCase;
                }
                break;
            default:
                if (!_ShowingMessage)
                {
                    var keyChar = key.ToCharacter();
                    if (keyChar != "")
                    {
                        var ch = keyChar[0];
                        if (IsLowerCase && ch >= 'A' && ch <= 'Z')
                        {
                            ch = char.ToLower(ch);
                        }
                        AddCharacter(ch);
                    }
                }
                break;
        }
    }

    public virtual void KeyUp(Key key)
    {
        switch (key)
        {
            case Key.Clr:
                if (_ClrPressedUtc != default)
                {
                    var threshold = _ClrPressedUtc.AddMilliseconds(HoldClrToEraseLineMilliseconds);
                    if (DateTime.UtcNow >= threshold)
                    {
                        Clear();
                    }
                }
                _ClrPressedUtc = default;
                break;
        }
    }

    public virtual void ShowErrorMessage(string message) => ShowMessage(message, Colour.Amber);

    public virtual void ShowInformativeMessage(string message) => ShowMessage(message, Colour.Cyan);

    public virtual void ShowFormatError() => ShowErrorMessage(" FORMAT ERROR");

    public virtual void ShowMessage(string message, Colour colour)
    {
        _MessageRow.Clear();
        var messageLength = Math.Min((message ?? "").Length, _MessageRow.Cells.Length);
        for (var idx = 0; idx < messageLength; ++idx)
        {
            var cell = _MessageRow.Cells[idx];
            cell.Colour = colour;
            SetCellCharacter(cell, message[idx]);
        }
        _ShowingMessage = true;
        OnRefreshRowDisplay();
    }

    public virtual void HideMessage()
    {
        _ShowingMessage = false;
        OnRefreshRowDisplay();
    }

    protected virtual void Backspace()
    {
        if (_Column > 0)
        {
            if (_Column < _DataEntryRow.Cells.Length - 1 || CurrentCell.Character == ' ')
            {
                --_Column;
            }
            CurrentCell.Clear();
            OnRefreshRowDisplay();
        }
    }

    public virtual void Clear()
    {
        _ShowingMessage = false;
        _DataEntryRow.Clear();
        _Column = 0;
        OnRefreshRowDisplay();
    }

    protected virtual void AddCharacter(char ch)
    {
        _LastCharacterAdded = DateTime.UtcNow;
        SetCellCharacter(CurrentCell, ch);
        AdvanceColumn();
        OnRefreshRowDisplay();
    }

    protected virtual void AddPlusMinus()
    {
        switch (PlusMinusBehaviour)
        {
            case ScratchpadPlusMinusBehaviour.AlwaysMinus:
                AddCharacter('-');
                break;
            case ScratchpadPlusMinusBehaviour.AlwaysPlus:
                AddCharacter('+');
                break;
            case ScratchpadPlusMinusBehaviour.ToggleMinusFirst:
            case ScratchpadPlusMinusBehaviour.TogglePlusFirst:
                var toggledPreviousCharacter = false;

                var previousCharacter = _Column == 0
                    ? ' '
                    : _DataEntryRow.Cells[_Column - 1].Character;
                if (previousCharacter == '+' || previousCharacter == '-')
                {
                    if (_LastCharacterAdded != default && _LastCharacterAdded.AddMilliseconds(TogglePlusMinusMilliseconds) > DateTime.UtcNow)
                    {
                        var character = previousCharacter == '+' ? '-' : '+';
                        _DataEntryRow.Cells[_Column - 1].Character = character;
                        OnRefreshRowDisplay();
                        toggledPreviousCharacter = true;
                    }
                }

                if (!toggledPreviousCharacter)
                {
                    AddCharacter(PlusMinusBehaviour == ScratchpadPlusMinusBehaviour.ToggleMinusFirst
                        ? '-'
                        : '+'
                    );
                }
                break;
        }
    }

    protected virtual void SetCellCharacter(Cell cell, char ch)
    {
        if (ShowLowercaseInSmallUppercase)
        {
            if (ch <= 'a' || ch >= 'z')
            {
                cell.Small = false;
            }
            else
            {
                cell.Small = true;
                ch = char.ToUpperInvariant(ch);
            }
        }
        cell.Character = ch;
    }

    protected virtual void AdvanceColumn()
    {
        _Column = Math.Min(_Column + 1, _DataEntryRow.Cells.Length - 1);
    }

    public string GetDataEntryText(bool trimmed = true)
    {
        var buffer = new StringBuilder();
        foreach (var cell in _DataEntryRow.Cells)
        {
            var ch = cell.Character;
            if (cell.Small)
            {
                ch = char.ToLowerInvariant(ch);
            }
            buffer.Append(ch);
        }

        var result = buffer.ToString();
        if (trimmed)
        {
            result = result.Trim();
        }

        return result;
    }
}