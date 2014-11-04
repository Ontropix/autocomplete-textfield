using System;
using MonoTouch.UIKit;
using MonoTouch.Foundation;
using System.Drawing;
using MonoTouch.CoreGraphics;

namespace Code9
{
    public interface  IAutocompleteDataSource
    {
        string CompletionForPrefix(string prefix);
    }

    public class AutocompleteTextField: UITextField
    {

        public event EventHandler DidAutoComplete = delegate {};

        private const int kHTAutoCompleteButtonWidth = 30;

        private UILabel autocompleteLabel;
        private UIButton autocompleteButton;
        private string autocompleteString;

        public IAutocompleteDataSource DataSource { get; set; }
        public bool ShowAutocompleteButton { get; set; }

        public AutocompleteTextField(System.Drawing.RectangleF frame)
            : base(frame)
        {
            Setup();
        }

        public override void AwakeFromNib()
        {
            base.AwakeFromNib();
            Setup();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                NSNotificationCenter.DefaultCenter.RemoveObserver(this, "UITextFieldTextDidChangeNotification");
            }
        }

        private void Setup()
        {
            //Label
            autocompleteLabel = new UILabel(System.Drawing.RectangleF.Empty);
            autocompleteLabel.Font = this.Font;
            autocompleteLabel.BackgroundColor = UIColor.Clear;
            autocompleteLabel.TextColor = UIColor.LightGray;
            autocompleteLabel.LineBreakMode = UILineBreakMode.Clip;
            autocompleteLabel.Hidden = true;

            this.AddSubview(autocompleteLabel);
            this.BringSubviewToFront(autocompleteLabel);

            //Button
            autocompleteButton = new UIButton(UIButtonType.Custom);
            autocompleteButton.AddTarget(OnButtonCliced, UIControlEvent.TouchUpInside);
            autocompleteButton.SetImage(UIImage.FromBundle("autocompleteButton"), UIControlState.Normal);

            this.AddSubview(autocompleteButton);
            this.BringSubviewToFront(autocompleteButton);

            autocompleteString = string.Empty;

            NSNotificationCenter.DefaultCenter.AddObserver("UITextFieldTextDidChangeNotification", OnUITextFieldTextDidChangeNotification);
        }

        public override void LayoutSubviews()
        {
            base.LayoutSubviews();
            this.autocompleteButton.Frame = this.FrameForAutocompleteButton();
        }

        public override bool BecomeFirstResponder()
        {
            BringSubviewToFront(autocompleteButton);

            if (this.ClearsOnBeginEditing)
            {
                this.autocompleteLabel.Text = string.Empty;
            }

            this.autocompleteLabel.Hidden = false;

            return base.BecomeFirstResponder();
        }

        public override bool ResignFirstResponder()
        {

            autocompleteLabel.Hidden = true;

            if (this.CommitAutocompleteText())
            {
                //Only notify if commiting autocomplete actually changed the text.
                NSNotificationCenter.DefaultCenter.PostNotificationName("UITextFieldTextDidChangeNotification", this);
            }

            return base.ResignFirstResponder();
        }
            

        private void OnButtonCliced(object sender, EventArgs args)
        {
            autocompleteLabel.Hidden = false;
            this.CommitAutocompleteText();
            NSNotificationCenter.DefaultCenter.PostNotificationName("UITextFieldTextDidChangeNotification", this);
        }
           

        public void UpdateAutocompleteLabel()
        {
            this.autocompleteLabel.Text = autocompleteString;
            autocompleteLabel.SizeToFit();
            autocompleteLabel.Frame = this.AutoCompleteRectForBounds(this.Bounds);

            //Fire the event
            DidAutoComplete(this, EventArgs.Empty);
        }

        public void OnUITextFieldTextDidChangeNotification(NSNotification notification)
        {
            this.RefreshAutocomleteText();
        }

        private System.Drawing.RectangleF FrameForAutocompleteButton ()
        {
            if (this.ClearButtonMode == UITextFieldViewMode.Never || this.Text.Length == 0)
            {
                return new System.Drawing.RectangleF(
                    this.Bounds.Size.Width - kHTAutoCompleteButtonWidth, 
                    this.Bounds.Size.Height / 2 - (this.Bounds.Height - 8) / 2,
                    kHTAutoCompleteButtonWidth, 
                    this.Bounds.Size.Height - 8
                );
            }

            return new System.Drawing.RectangleF(
                this.Bounds.Size.Width - kHTAutoCompleteButtonWidth - 25,
                this.Bounds.Size.Height / 2 - (this.Bounds.Height - 8) / 2,
                kHTAutoCompleteButtonWidth,
                this.Bounds.Size.Height - 8
            );
        }

        private System.Drawing.RectangleF AutoCompleteRectForBounds(RectangleF bounds) 
        {
            // get bounds for whole text area
            RectangleF textRectBounds = this.TextRect(bounds);

            if (this.BeginningOfDocument == null)
                return RectangleF.Empty;

            // get rect for actual text
            UITextRange textRange = this.GetTextRange(this.BeginningOfDocument, this.EndOfDocument);

            RectangleF textRect = this.GetFirstRectForRange(textRange).Integral();

            NSMutableParagraphStyle paragraphStyle = new NSMutableParagraphStyle();
            paragraphStyle.LineBreakMode = UILineBreakMode.CharacterWrap;

            NSString text = new NSString(this.Text);

            UIStringAttributes attributes = new UIStringAttributes();
            attributes.Font = this.Font;
            attributes.ParagraphStyle = paragraphStyle;

            RectangleF prefixTextRect  = text.GetBoundingRect(
                textRect.Size, 
                NSStringDrawingOptions.UsesLineFragmentOrigin | NSStringDrawingOptions.UsesFontLeading, 
                attributes, null
            );

            SizeF prefixTextSize = prefixTextRect.Size;

            NSString str = new NSString(autocompleteString);

            RectangleF autocompleteTextRect = str.GetBoundingRect(
                new SizeF(textRectBounds.Size.Width - prefixTextSize.Width, textRectBounds.Size.Height),
                NSStringDrawingOptions.UsesLineFragmentOrigin | NSStringDrawingOptions.UsesFontLeading, 
                attributes, null
            );

            SizeF autocompleteTextSize = autocompleteTextRect.Size;

            return new RectangleF(textRect.GetMaxX() + 6, //6 - correction
                textRectBounds.GetMinY() - 1, //1 - correction
                autocompleteTextSize.Width,
                textRectBounds.Size.Height);
        }


        private void RefreshAutocomleteText() 
        {
            this.autocompleteString = DataSource.CompletionForPrefix(this.Text);

            if (autocompleteString.Length > 0)
            {
                if (this.Text.Length == 0 || this.Text.Length == 1)
                {
                    this.UpdateAutocompleteButtonAnimated(true);
                }
            }

            this.UpdateAutocompleteLabel();
        }

        private bool CommitAutocompleteText()
        {
            string currentText = this.Text;

            if (!string.IsNullOrEmpty(autocompleteString))
            {
                this.Text = this.Text + this.autocompleteString;

                autocompleteString = string.Empty;
                this.UpdateAutocompleteLabel();             
                DidAutoComplete(this, EventArgs.Empty);
            }

            return currentText != this.Text;
        }

        private void UpdateAutocompleteButtonAnimated(bool animated) 
        {
            NSAction action = new NSAction(() =>
            {
                    if (this.autocompleteString.Length > 0 && this.ShowAutocompleteButton) 
                    {
                        this.autocompleteButton.Alpha = 1;
                        this.autocompleteButton.Frame = this.FrameForAutocompleteButton();
                    }
                    else
                    {
                        this.autocompleteButton.Alpha = 0;
                    }
            });

            if (animated)
            {
                UIView.Animate(0.15f, action);
            }
            else
            {
                action();
            }
        }
     
    }
}

