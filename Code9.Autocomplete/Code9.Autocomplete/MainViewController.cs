using System;
using System.Drawing;

using MonoTouch.Foundation;
using MonoTouch.UIKit;
using System.Linq;

namespace Code9.Autocomplete
{
    public partial class MainViewController : UIViewController
    {
        public MainViewController(IntPtr handle)
            : base(handle)
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            AutocompleteTextField textField = new AutocompleteTextField(new RectangleF(10, 100, 300, 30));
            textField.DataSource = new DefaultDataSource();
            textField.BorderStyle = UITextBorderStyle.RoundedRect;
            textField.ShowAutocompleteButton = true;

            View.Add(textField);
        }
            
    }

    public class DefaultDataSource: IAutocompleteDataSource 
    {
        public string[] domains = new string[] {"google", "yandex", "bing"};

        public string CompletionForPrefix(string prefix)
        {
            if (string.IsNullOrEmpty(prefix))
            {
                return domains[0];
            }

            string domain = domains.FirstOrDefault(x => x.StartsWith(prefix, true, System.Globalization.CultureInfo.InvariantCulture));

            return string.IsNullOrEmpty(domain) ? string.Empty : domain.Remove(0, prefix.Length);
        }
    }
}

