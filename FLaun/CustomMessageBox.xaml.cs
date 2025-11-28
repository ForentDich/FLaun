using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace FLaun
{
    /// <summary>
    /// Логика взаимодействия для CustomMessageBox.xaml
    /// </summary>
    public partial class CustomMessageBox : Window
    {
        public enum CustomMessageBoxResult
        {
            None,
            OK,
            Cancel
        }

        public CustomMessageBoxResult Result { get; private set; } = CustomMessageBoxResult.None;

        public CustomMessageBox(string message, MessageBoxButton buttons)
        {
            InitializeComponent();
            messageTextBlock.Text = message;
            AddButtons(buttons);
        }

        private void AddButtons(MessageBoxButton buttons)
        {
            switch (buttons)
            {
                case MessageBoxButton.OK:
                    AddButton("OK", CustomMessageBoxResult.OK);
                    break;
                case MessageBoxButton.OKCancel:
                    AddButton("OK", CustomMessageBoxResult.OK);
                    AddButton("Отмена", CustomMessageBoxResult.Cancel);
                    break;
            }
        }

        private void AddButton(string content, CustomMessageBoxResult result)
        {
            Button button = new Button
            {
                Content = content,
                Width = 100,
                Margin = new Thickness(5),
                FontSize = 20,
                FontFamily = new FontFamily("Bahnschrift"),
                FontWeight = FontWeights.SemiBold,
                Height = 30,
                VerticalContentAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right,
                Style = (Style)FindResource("StyleButton_3"),
        };

            button.Click += (sender, e) =>
            {
                Result = result;
                DialogResult = true;
                Close();
            };
            ButtonPanel.Children.Add(button);
        }

        public static CustomMessageBoxResult Show(string message, MessageBoxButton buttons = MessageBoxButton.OK)
        {
            CustomMessageBox box = new CustomMessageBox(message, buttons);
            box.ShowDialog();
            return box.Result;
        }
    }
}
