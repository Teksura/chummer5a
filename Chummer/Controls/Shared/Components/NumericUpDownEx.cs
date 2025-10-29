/*  This file is part of Chummer5a.
 *
 *  Chummer5a is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  Chummer5a is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with Chummer5a.  If not, see <http://www.gnu.org/licenses/>.
 *
 *  You can obtain the full source code for Chummer5a at
 *  https://github.com/chummer5a/chummer5a
 */
// ============================================================================ '
// NumericUpDownEx - v.1.6                                                      '
// ============================================================================ '
// Author:   Claudio Nicora                                                     '
// WebSite:  http://coolsoft.altervista.org/numericupdownex                     '
// CodeProject: http://www.codeproject.com/KB/edit/NumericUpDownEx.aspx         '
// License:  CodeProject Open License                                           '
//           http://www.codeproject.com/info/cpol10.aspx                        '
// Feel free to contribute here: http://coolsoft.altervista.org                 '
// ============================================================================ '

using System;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace Chummer
{
    public class NumericUpDownEx : NumericUpDown
    {
        // reference to the underlying TextBox control
        private readonly TextBox _textbox;

        // reference to the underlying UpDownButtons control
        private readonly Control _upDownButtons;

        // default value that will be used when incrementing via mousewheel
        private int _intMouseIncrement = 1;

        /// <summary>
        /// object creator
        /// </summary>
        public NumericUpDownEx() : base()
        {
            // get a reference to the underlying UpDownButtons field
            // Underlying private type is System.Windows.Forms.UpDownBase+UpDownButtons
            _upDownButtons = Controls[0];
            if (_upDownButtons == null || _upDownButtons.GetType().FullName != "System.Windows.Forms.UpDownBase+UpDownButtons")
            {
                throw new ArgumentNullException(GetType().FullName, "Can't find internal UpDown buttons field.");
            }
            // Get a reference to the underlying TextBox field.
            // Underlying private type is System.Windows.Forms.UpDownBase+UpDownButtons
            _textbox = Controls[1] as TextBox;
            if (_textbox == null || _textbox.GetType().FullName != "System.Windows.Forms.UpDownBase+UpDownEdit")
            {
                throw new ArgumentNullException(GetType().FullName, "Can't find internal TextBox field.");
            }
            // add handlers (MouseEnter and MouseLeave events of NumericUpDown
            // are not working properly)
            _textbox.MouseEnter += _mouseEnterLeave;
            _textbox.MouseLeave += _mouseEnterLeave;
            _textbox.KeyDown += txt_KeyDown;
            _upDownButtons.MouseEnter += _mouseEnterLeave;
            _upDownButtons.MouseLeave += _mouseEnterLeave;
        }

        private int _intSkipOnMarginChanged;
        private float _fltMyDpiX = 96.0f;
        private float _fltMyDpiY = 96.0f;
        private Padding _objSavedMargins;
        private bool _blnMarginsSaved;

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _mouseEnterLeave(this, e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseEnter(e);
            _mouseEnterLeave(this, e);
        }

        // DPI handler for margins (WinForms is buggy with handling scaling of margins on numeric up-downs)
        protected override void OnParentChanged(EventArgs e)
        {
            base.OnParentChanged(e);
            OnMarginChangedCommon();
        }
        protected override void OnMarginChanged(EventArgs e)
        {
            base.OnMarginChanged(e);
            OnMarginChangedCommon();
        }
        protected override void OnDpiChangedAfterParent(EventArgs e)
        {
            base.OnDpiChangedAfterParent(e);
            OnMarginChangedCommon();
        }

        private void OnMarginChangedCommon()
        {
            if (_intSkipOnMarginChanged > 0)
                return;
            try
            {
                using (Graphics g = CreateGraphics())
                {
                    // Only care about adjusting margins for non-standard DPI
                    if (Math.Abs(g.DpiX - _fltMyDpiX) < float.Epsilon &&
                        Math.Abs(g.DpiY - _fltMyDpiY) < float.Epsilon)
                    {
                        if (_blnMarginsSaved)
                        {
                            Interlocked.Increment(ref _intSkipOnMarginChanged);
                            Margin = _objSavedMargins;
                        }
                        return;
                    }
                    float fltOldDpiX = _fltMyDpiX;
                    float fltOldDpiY = _fltMyDpiY;
                    _fltMyDpiX = g.DpiX;
                    _fltMyDpiY = g.DpiY;
                    // No non-zero margins, exit
                    if (Margin.All == 0)
                        return;
                    Padding objNewMargins = new Padding(
                        (Margin.Left * _fltMyDpiX / fltOldDpiX).StandardRound(),
                        (Margin.Top * _fltMyDpiY / fltOldDpiY).StandardRound(),
                        (Margin.Right * _fltMyDpiX / fltOldDpiX).StandardRound(),
                        (Margin.Bottom * _fltMyDpiY / fltOldDpiY).StandardRound());
                    if (objNewMargins.Equals(Margin))
                        return;
                    Interlocked.Increment(ref _intSkipOnMarginChanged);
                    Margin = objNewMargins;
                }
            }
            finally
            {
                _objSavedMargins = Margin;
                _blnMarginsSaved = true;
                Interlocked.Decrement(ref _intSkipOnMarginChanged);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (!_upDownButtons.Visible)
            {
                e.Graphics.Clear(BackColor);
            }
            base.OnPaint(e);
        }

        private void txt_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                if (!Parent.Parent.Focus())
                {
                    Utils.BreakIfDebug();
                }
                e.SuppressKeyPress = true;
                e.Handled = true;
            }
        }

        /// <summary>
        /// WndProc override to kill WN_MOUSEWHEEL message
        /// </summary>
        protected override void WndProc(ref Message m)
        {
            const int WM_MOUSEWHEEL = 0x20a;

            if (m.Msg == WM_MOUSEWHEEL)
            {
                switch (_interceptMouseWheel)
                {
                    case InterceptMouseWheelMode.Always:
                        // standard message
                        base.WndProc(ref m);
                        break;

                    case InterceptMouseWheelMode.WhenMouseOver:
                        if (_mouseOver)
                        {
                            // standard message
                            base.WndProc(ref m);
                        }
                        break;

                    case InterceptMouseWheelMode.WhenFocus:
                        if (_haveFocus)
                        {
                            base.WndProc(ref m);
                        }
                        break;

                    case InterceptMouseWheelMode.Never:
                        // kill the message
                        return;
                }
            }
            else
            {
                base.WndProc(ref m);
            }
        }

        #region New properties

        [DefaultValue(false)]
        [Category("Behavior")]
        [Description("Automatically select control text when it receives focus.")]
        public bool AutoSelect { get; set; }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int SelectionStart
        {
            get => _textbox.SelectionStart;
            set => _textbox.SelectionStart = value;
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int SelectionLength
        {
            get => _textbox.SelectionLength;
            set => _textbox.SelectionLength = value;
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string SelectedText
        {
            get => _textbox.SelectedText;
            set => _textbox.SelectedText = value;
        }

        [DefaultValue(typeof(InterceptMouseWheelMode), "Always")]
        [Category("Behavior")]
        [Description("Enables MouseWheel only under certain conditions.")]
        public InterceptMouseWheelMode InterceptMouseWheel
        {
            get => _interceptMouseWheel;
            set => _interceptMouseWheel = value;
        }

        private InterceptMouseWheelMode _interceptMouseWheel = InterceptMouseWheelMode.Always;

        public enum InterceptMouseWheelMode
        {
            /// <summary>MouseWheel always works (defauld behavior)</summary>
            Always,

            /// <summary>MouseWheel works only when mouse is over the (focused) control</summary>
            WhenMouseOver,

            /// <summary>UpDownButtons are visible only when control has focus</summary>
            WhenFocus,

            /// <summary>MouseWheel never works</summary>
            Never
        }

        [Category("Behavior")]
        [DefaultValue(typeof(ShowUpDownButtonsMode), "Always")]
        [Description("Set UpDownButtons visibility mode.")]
        public ShowUpDownButtonsMode ShowUpDownButtons
        {
            get => _showUpDownButtons;
            set
            {
                if (InterlockedExtensions.Exchange(ref _showUpDownButtons, value) != value)
                    // update UpDownButtons visibility
                    UpdateUpDownButtonsVisibility();
            }
        }

        private ShowUpDownButtonsMode _showUpDownButtons = ShowUpDownButtonsMode.Always;

        public enum ShowUpDownButtonsMode
        {
            /// <summary>UpDownButtons are always visible (defauld behavior)</summary>
            Always,

            /// <summary>UpDownButtons are visible only when mouse is over the control</summary>
            WhenMouseOver,

            /// <summary>UpDownButtons are visible only when control has focus</summary>
            WhenFocus,

            /// <summary>UpDownButtons are visible when control has focus or mouse is over it</summary>
            WhenFocusOrMouseOver,

            /// <summary>UpDownButtons are never visible</summary>
            Never
        }

        /// <summary>
        /// If set, incrementing value will cause it to restart from Minimum
        /// when Maximum is reached (and viceversa).
        /// </summary>
        [Category("Behavior")]
        [DefaultValue(1)]
        [Description("Indicates the amount to increment or decrement with the use of the mouse wheel.")]
        public int MouseIncrement
        {
            get => _intMouseIncrement;
            set => _intMouseIncrement = value;
        }

        /// <summary>
        /// If set, incrementing value will cause it to restart from Minimum
        /// when Maximum is reached (and viceversa).
        /// </summary>
        [Category("Behavior")]
        [DefaultValue(false)]
        [Description("If set, incrementing value will cause it to restart from Minimum when Maximum is reached (and viceversa).")]
        public bool WrapValue
        {
            get => _wrapValue;
            set => _wrapValue = value;
        }

        private bool _wrapValue;

        #endregion New properties

        #region Text selection

        // select all the text on focus enter
        protected override void OnGotFocus(EventArgs e)
        {
            _haveFocus = true;
            if (AutoSelect)
            {
                _textbox.SelectAll();
            }
            // Update UpDownButtons visibility
            if (_showUpDownButtons == ShowUpDownButtonsMode.WhenFocus || _showUpDownButtons == ShowUpDownButtonsMode.WhenFocusOrMouseOver)
            {
                UpdateUpDownButtonsVisibility();
            }
            base.OnGotFocus(e);
        }

        // indicate that we have lost the focus
        protected override void OnLostFocus(EventArgs e)
        {
            _haveFocus = false;
            // Update UpDownButtons visibility
            if (_showUpDownButtons == ShowUpDownButtonsMode.WhenFocus || _showUpDownButtons == ShowUpDownButtonsMode.WhenFocusOrMouseOver)
            {
                UpdateUpDownButtonsVisibility();
            }
            base.OnLostFocus(e);
        }

        // MouseUp will kill the SelectAll made on GotFocus.
        // Will restore it, but only if user have not made a partial text selection.
        protected override void OnMouseUp(MouseEventArgs mevent)
        {
            if (AutoSelect && _textbox.SelectionLength == 0)
            {
                _textbox.SelectAll();
            }
            base.OnMouseUp(mevent);
        }

        #endregion Text selection

        #region Additional events

        // these events will be raised correctly, when mouse enters on the textbox
        public new event EventHandler<EventArgs> MouseEnter;

        public new event EventHandler<EventArgs> MouseLeave;

        // Events raised BEFORE value decrement(increment
        public event CancelEventHandler BeforeValueDecrement;

        public event CancelEventHandler BeforeValueIncrement;

        // flag to track mouse position
        private bool _mouseOver;

        // flag to track focus
        private bool _haveFocus;

        // this handler is called at each mouse Enter/Leave movement
        private void _mouseEnterLeave(object sender, EventArgs e)
        {
            Rectangle cr = RectangleToScreen(ClientRectangle);
            Point mp = MousePosition;

            // actual state
            bool isOver = cr.Contains(mp);

            // test if status changed
            if (_mouseOver ^ isOver)
            {
                // update state
                _mouseOver = isOver;
                if (_mouseOver)
                {
                    MouseEnter?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    MouseLeave?.Invoke(this, EventArgs.Empty);
                }
            }

            // update UpDownButtons visibility
            if (_showUpDownButtons != ShowUpDownButtonsMode.Always)
            {
                UpdateUpDownButtonsVisibility();
            }
        }

        #endregion Additional events

        #region Value increment/decrement management

        // raises the two new events
        public override void DownButton()
        {
            if (ReadOnly) return;
            CancelEventArgs e = new CancelEventArgs();
            BeforeValueDecrement?.Invoke(this, e);
            if (e.Cancel) return;

            if (_wrapValue && Value - Increment < Minimum)
            {
                Value = Maximum;
            }
            else
            {
                try
                {
                    base.DownButton();
                }
                catch (ArgumentOutOfRangeException aor)
                {
                    aor.Data.Add("Name", Name);
                    aor.Data.Add("Parent", Parent?.Name);
                    aor.Data.Add("BeforeValueDecrement", BeforeValueDecrement?.Method.Name);
                    throw;
                }
            }
        }

        public override void UpButton()
        {
            if (ReadOnly) return;
            CancelEventArgs e = new CancelEventArgs();
            BeforeValueIncrement?.Invoke(this, e);
            if (e.Cancel) return;

            if (_wrapValue && Value + Increment > Maximum)
            {
                Value = Minimum;
            }
            else
            {
                try
                {
                    base.UpButton();
                }
                catch (ArgumentOutOfRangeException aor)
                {
                    aor.Data.Add("Name", Name);
                    aor.Data.Add("Parent", Parent?.Name);
                    aor.Data.Add("BeforeValueIncrement", BeforeValueIncrement?.Method.Name);
                    throw;
                }
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            if (e is HandledMouseEventArgs hme)
                hme.Handled = true;

            if (e.Delta > 0)
                Value = Math.Min(Value + MouseIncrement, Maximum);
            else if (e.Delta < 0)
                Value = Math.Max(Value - MouseIncrement, Minimum);
        }

        #endregion Value increment/decrement management

        #region Integer Value Stuff

        private int _intValue;
        private int _intMinimum;
        private int _intMaximum;
        private readonly object _objIntValueLock = new object();

        protected override void OnValueChanged(EventArgs e)
        {
            using (CursorWait.New(this))
            {
                lock (_objIntValueLock) // Lock ensures synchronicity
                    _intValue = Math.Min(Math.Max(Value, int.MinValue), int.MaxValue).ToInt32();
                base.OnValueChanged(e);
            }
        }

        /// <summary>
        /// Cached expression of Value as an integer
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Browsable(false)]
        public int ValueAsInt
        {
            get
            {
                lock (_objIntValueLock) // Lock ensures synchronicity
                    return _intValue;
            }
            set
            {
                // Ensure value is within bounds before setting
                int intAdjustedValue = Math.Max(Math.Min(value, (int)Maximum), (int)Minimum);
                Value = intAdjustedValue;
            }
        }

        /// <summary>
        /// Cached expression of Minimum as an integer
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Browsable(false)]
        public int MinimumAsInt
        {
            get => _intMinimum;
            set => Minimum = value;
        }

        /// <summary>
        /// The minimum allowed value of the spin box.
        /// </summary>
        public new decimal Minimum
        {
            get => base.Minimum;
            set
            {
                if (value == base.Minimum)
                    return;
                _intMinimum = Math.Min(Math.Max(value, int.MinValue), int.MaxValue).ToInt32();
                base.Minimum = value;
            }
        }

        /// <summary>
        /// Cached expression of Minimum as an integer
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Browsable(false)]
        public int MaximumAsInt
        {
            get => _intMaximum;
            set => Maximum = value;
        }

        /// <summary>
        /// The maximum allowed value of the spin box.
        /// </summary>
        public new decimal Maximum
        {
            get => base.Maximum;
            set
            {
                if (value == base.Maximum)
                    return;
                _intMaximum = Math.Min(Math.Max(value, int.MinValue), int.MaxValue).ToInt32();
                base.Maximum = value;
            }
        }

        #endregion Integer Value Stuff

        #region UpDownButtons visibility management

        /// <summary>
        /// Show or hide the UpDownButtons, according to ShowUpDownButtons property value
        /// </summary>
        public void UpdateUpDownButtonsVisibility()
        {
            // test new state
            bool newVisible;
            switch (_showUpDownButtons)
            {
                case ShowUpDownButtonsMode.WhenMouseOver:
                    newVisible = _mouseOver;
                    break;

                case ShowUpDownButtonsMode.WhenFocus:
                    newVisible = _haveFocus;
                    break;

                case ShowUpDownButtonsMode.WhenFocusOrMouseOver:
                    newVisible = _mouseOver || _haveFocus;
                    break;

                case ShowUpDownButtonsMode.Never:
                    newVisible = false;
                    break;

                default:
                    newVisible = true;
                    break;
            }

            // assign only if needed
            if (_upDownButtons.Visible != newVisible)
            {
                if (newVisible)
                {
                    _textbox.Width = ClientRectangle.Width - _upDownButtons.Width;
                }
                else
                {
                    _textbox.Width = ClientRectangle.Width;
                }
                _upDownButtons.Visible = newVisible;
                OnTextBoxResize(_textbox, EventArgs.Empty);
                Invalidate();
            }
        }

        /// <summary>
        /// Custom textbox size management
        /// </summary>
        protected override void OnTextBoxResize(object source, EventArgs e)
        {
            if (_textbox == null)
                return;
            if (_showUpDownButtons == ShowUpDownButtonsMode.Always)
            {
                // standard management
                base.OnTextBoxResize(source, e);
            }
            else
            {
                // custom management

                // change position if RTL
                bool fixPos = RightToLeft == RightToLeft.Yes ^ UpDownAlign == LeftRightAlignment.Left;

                using (Graphics g = CreateGraphics())
                {
                    int int2PxWidth = (int)(2 * g.DpiX / 96.0f);
                    if (_mouseOver)
                    {
                        _textbox.Width = ClientSize.Width - _textbox.Left - _upDownButtons.Width - int2PxWidth;
                        if (fixPos)
                        {
                            int int16PxWidth = (int)(16 * g.DpiX / 96.0f);
                            _textbox.Location = new Point(int16PxWidth, _textbox.Location.Y);
                        }
                    }
                    else
                    {
                        if (fixPos)
                            _textbox.Location = new Point(int2PxWidth, _textbox.Location.Y);
                        _textbox.Width = ClientSize.Width - _textbox.Left - int2PxWidth;
                    }
                }
            }
        }

        #endregion UpDownButtons visibility management

        #region Safe Value Setting Methods
        #region Integer input
        /// <summary>
        /// Safely sets the value, ensuring it stays within the minimum and maximum bounds.
        /// </summary>
        /// <param name="value">The value to set</param>
        public void SetValueSafely(int value)
        {
            int intAdjustedValue = Math.Max(Math.Min(value, (int)Maximum), (int)Minimum);
            Value = intAdjustedValue;
        }

        /// <summary>
        /// Safely sets the value with a known maximum, ensuring it stays within the minimum and maximum bounds.
        /// </summary>
        /// <param name="value">The value to set</param>
        /// <param name="maximum">The maximum value to enforce</param>
        public void SetValueSafely(int value, int maximum)
        {
            int intAdjustedValue = Math.Max(Math.Min(value, maximum), (int)Minimum);
            Value = intAdjustedValue;
        }

        /// <summary>
        /// Safely adjusts the value by a given amount, ensuring it stays within the minimum and maximum bounds.
        /// </summary>
        /// <param name="adjustment">The amount to adjust the value by (can be negative)</param>
        public void AdjustValueSafely(int adjustment)
        {
            decimal newValue = Math.Max(Value - adjustment, 0);
            if (newValue > Maximum)
            {
                newValue = Maximum;
            }
            if (newValue < Minimum)
            {
                newValue = Minimum;
            }
            Value = newValue;
        }
        #endregion
        #region Decimal input
        /// <summary>
        /// Safely sets the value, ensuring it stays within the minimum and maximum bounds.
        /// </summary>
        /// <param name="value">The value to set</param>
        public void SetValueSafely(decimal value)
        {
            decimal decAdjustedValue = Math.Max(Math.Min(value, Maximum), Minimum);
            Value = decAdjustedValue;
        }

        /// <summary>
        /// Safely sets the value with a known maximum, ensuring it stays within the minimum and maximum bounds.
        /// </summary>
        /// <param name="value">The value to set</param>
        /// <param name="maximum">The maximum value to enforce</param>
        public void SetValueSafely(decimal value, decimal maximum)
        {
            decimal decAdjustedValue = Math.Max(Math.Min(value, maximum), Minimum);
            Value = decAdjustedValue;
        }

        /// <summary>
        /// Safely adjusts the value by a given amount, ensuring it stays within the minimum and maximum bounds.
        /// </summary>
        /// <param name="adjustment">The amount to adjust the value by (can be negative)</param>
        public void AdjustValueSafely(decimal adjustment)
        {
            decimal newValue = Math.Max(Value - adjustment, 0);
            if (newValue > Maximum)
            {
                newValue = Maximum;
            }
            if (newValue < Minimum)
            {
                newValue = Minimum;
            }
            Value = newValue;
        }
        #endregion
        #endregion Safe Value Setting Methods
    }
}
