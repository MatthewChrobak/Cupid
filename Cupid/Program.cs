using System;

namespace Cupid
{
    public class Program
    {
        [STAThread]
        public static void Main(string[] args) {


#if DEBUG
            args = new string[] {
                @"C:\Users\Matthew\Dropbox\Pixel Art\Pixel Art\1.bmp"
            };
#endif
            // Make sure that we have input.
            if (args.Length > 0) {
                // Assume the filepath is the first entry.
                string filePath = args[0];

                // Create a new window to display images in.
                var window = new MainWindow(filePath);

                try {
                    // Show the window.
                    window.ShowDialog();
                } catch (InvalidOperationException e) {
                    // Exit with an error code of 1.
                    Environment.Exit(1);
                }

                // Exit normally.
                Environment.Exit(0);
            }
        }
    }
}
