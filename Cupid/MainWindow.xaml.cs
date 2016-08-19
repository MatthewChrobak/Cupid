using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Cupid
{
    public partial class MainWindow : Window
    {
        private string[] _knownFormats = new string[] {
            ".jpg",
            ".jpeg",
            ".bmp",
            ".png"
        };

        private bool _dragMode;
        private Point _startDragPoint;
        private FileSystemWatcher _fileWatcher;
        private List<string> _files;
        private int _currentFileIndex;

        public MainWindow(string filePath) {
            // Initialize the window.
            this.InitializeComponent();

            // Initialize the event handlers.
            Window.GetWindow(this).KeyDown += HandleKeyDown;
            this.Loaded += (sender, e) => {
                // Load the image once the window is visible.
                this.LoadImage(filePath);
            };
        }


        private void LoadImage(string filePath) {
            // Make sure the file exists.
            if (File.Exists(filePath)) {
                var fi = new FileInfo(filePath);

                // Initialize the file watcher system if it is not yet implemented.
                if (this._fileWatcher == null || this._files == null) {

                    // Create a new file watcher to look for files added or removed.
                    this._fileWatcher = new FileSystemWatcher(fi.Directory.FullName);
                    this._fileWatcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite;
                    this._fileWatcher.EnableRaisingEvents = true;
                    this._fileWatcher.IncludeSubdirectories = false;

                    // Create a new list of known files, and find the index of the current file.
                    this._files = Directory.GetFiles(this._fileWatcher.Path).Where(file => this._knownFormats.Contains(file.Remove(0, file.LastIndexOf('.')))).ToList();
                    this._currentFileIndex = this._files.IndexOf(fi.FullName);


                    // Create event handlers for the file system watcher.
                    this._fileWatcher.Deleted += (sender, e) => {

                        // Make sure the file is an actual image file that we're tracking.
                        if (this._knownFormats.Contains(e.FullPath.Remove(0, e.FullPath.LastIndexOf('.')))) {
                            // Remove the file from the collection.
                            this._files.Remove(e.FullPath);

                            // Modify the current index, if needed.
                            if (this._files[this._currentFileIndex].CompareTo(e.FullPath) > 0) {
                                this._currentFileIndex--;
                            }
                        }
                    };
                    this._fileWatcher.Created += (sender, e) => {

                        // Make sure the file is an actual image file.
                        if (this._knownFormats.Contains(e.FullPath.Remove(0, e.FullPath.LastIndexOf('.')))) {
                            // Add the file to the collection, and sort it.
                            this._files.Add(e.FullPath);
                            this._files.Sort();

                            // Modify the current index, if needed.
                            if (this._files[this._currentFileIndex].CompareTo(e.FullPath) > 0) {
                                this._currentFileIndex++;
                            }
                        }
                    };
                    this._fileWatcher.Renamed += (sender, e) => {

                        // Was the original file contained within our local collection?
                        if (this._knownFormats.Contains(e.OldFullPath.Remove(0, e.OldFullPath.LastIndexOf('.')))) {

                            // Get the index of the old file within the collection, and remove it.
                            int oldIndex = this._files.IndexOf(e.OldFullPath);
                            this._files.RemoveAt(oldIndex);

                            // Is the new file a valid image file?
                            if (this._knownFormats.Contains(e.FullPath.Remove(0, e.FullPath.LastIndexOf('.')))) {
                                // Add the new path, sort the collection, and get the new index.
                                this._files.Add(e.FullPath);
                                this._files.Sort();
                                int newIndex = this._files.IndexOf(e.FullPath);

                                // Re-align our current file index.
                                if (oldIndex < this._currentFileIndex && newIndex >= this._currentFileIndex) {
                                    this._currentFileIndex--;
                                }
                                if (oldIndex > this._currentFileIndex && newIndex <= this._currentFileIndex) {
                                    this._currentFileIndex++;
                                }
                            } else {

                                // The new file is not a valid image file. The extension was changed via renaming.
                                // Re-align our current file index.
                                if (oldIndex < this._currentFileIndex) {
                                    this._currentFileIndex--;
                                } else {
                                    this._currentFileIndex++;
                                }
                            }

                            // If it wasn't before, is it now?
                        } else if (this._knownFormats.Contains(e.FullPath.Remove(0, e.FullPath.LastIndexOf('.')))) {
                            // Add the new file, sort the collection, and get the new index.
                            this._files.Add(e.FullPath);
                            this._files.Sort();
                            int newIndex = this._files.IndexOf(e.FullPath);

                            // Re-align our current file index.
                            if (newIndex < this._currentFileIndex) {
                                this._currentFileIndex++;
                            }
                        }
                    };
                }

                // Make sure the extension is a valid one.
                if (!this._knownFormats.Contains(fi.Extension.ToLower())) {
                    throw new FileFormatException(string.Format("Unknown file format {0}", fi.Extension));
                }

                this.Dispatcher.BeginInvoke(new ThreadStart(() => {
                    // Load the bitmap.
                    var bitmap = new BitmapImage(new Uri(filePath));

                    // Set the bitmap as the image source.
                    this.imageDisplay.Source = bitmap;

                    // Reset the margin for the image, so it's properly centered.
                    this.imageWrapper.Margin = new Thickness(0, 0, 0, 0);

                    // Calculate a ratio for later user.
                    double ratio = bitmap.PixelHeight / (double)bitmap.PixelWidth;

                    // Figure out what we should scale first.
                    if (bitmap.PixelHeight >= bitmap.PixelWidth) {
                        this.imageWrapper.Height = this.ActualHeight / ratio;
                        this.imageWrapper.Width = this.imageWrapper.Height / ratio;
                    } else {
                        this.imageWrapper.Width = this.ActualWidth * ratio;
                        this.imageWrapper.Height = this.imageWrapper.Width * ratio;
                    }

                    // Set the title.
                    this.Title = fi.Name;
                }));
            } else {
                throw new FileNotFoundException(string.Format("Tried to load {0}", filePath));
            }
        }

        private void HandleMouseWheel(object sender, MouseWheelEventArgs e) {
            // Get the zoom change.
            double dt = e.Delta > 0 ? 0.02 : -0.02;

            // Apply the change.
            this.imageWrapper.Width += this.imageWrapper.Width * dt;
            this.imageWrapper.Height += this.imageWrapper.Height * dt;
        }

        private void HandleKeyDown(object sender, KeyEventArgs e) {
            // Figure out what key was pressed.
            switch (e.Key) {
                case Key.Left:
                    // Load the previous image in the collection.
                    this.LoadImage(this._files[(--this._currentFileIndex + this._files.Count) % this._files.Count]);
                    break;
                case Key.Right:
                    // Load the next image in the collection.
                    this.LoadImage(this._files[(++this._currentFileIndex + this._files.Count) % this._files.Count]);
                    break;
                case Key.Space:
                    // Reset the wrapper margin.
                    this.imageWrapper.Margin = new Thickness(0, 0, 0, 0);
                    break;
            }
        }

        private void HandleMouseUp(object sender, MouseButtonEventArgs e) {
            // Make sure the proper button was released.
            if (e.ChangedButton == MouseButton.Left) {
                // Toggle the drag-mode.
                this._dragMode = false;
            }
        }

        private void HandleMouseDown(object sender, MouseButtonEventArgs e) {
            // Make sure the proper button was clicked.
            if (e.ChangedButton == MouseButton.Left) {
                // Toggle the drag-mode.
                this._dragMode = true;

                // Set the mouse position to use for modifying the margin.
                var pos = Mouse.GetPosition(this);
                this._startDragPoint.X = this.imageWrapper.Margin.Left - pos.X;
                this._startDragPoint.Y = this.imageWrapper.Margin.Top - pos.Y;
            }
        }

        private void HandleMouseMove(object sender, MouseEventArgs e) {
            // Make sure we're in drag-mode.
            if (this._dragMode) {
                // Get the current position of the mouse, and the current margin.
                var pos = Mouse.GetPosition(this);
                var margin = this.imageWrapper.Margin;

                // Move it based on how far the mouse has moved.
                margin.Left = (this._startDragPoint.X + pos.X);
                margin.Top = (this._startDragPoint.Y + pos.Y);

                // Apply the new margin.
                this.imageWrapper.Margin = margin;
            }
        }
    }
}
