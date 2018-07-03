using System;
using System.Xml;
//using System.IO;
using System.Text;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows;

namespace FlaticonSvgToXaml
{
    class MainViewModel : INotifyPropertyChanged
    {
        public ICommand ClickTransformCommand { get; set; }

        public Canvas Canvas { get; set; }

        public Geometry IconPreview { get { return iconPreview; } set { iconPreview = value; OnPropertyChanged("IconPreview"); } }
        public string SvgText { get { return svgText; } set { svgText = value; OnPropertyChanged("SvgText"); } }
        public string XamlText { get { return xamlText; } set { xamlText = value; OnPropertyChanged("XamlText"); } }

        private Geometry iconPreview;
        private string svgText;
        private string xamlText;

        private Rect viewport;

        public MainViewModel()
        {
            ClickTransformCommand = new RelayCommand(ClickTransformHandler);
        }

        private void ClickTransformHandler(object param)
        {
            XmlTextReader reader = new XmlTextReader(GenerateStreamFromString(svgText));

            string nodeName = null;
            bool noChilds = false;
            try
            {
                while (reader.Read())
                {
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            nodeName = reader.Name;
                            noChilds = reader.IsEmptyElement;

                            NewElement(reader.Name);
                            while (reader.MoveToNextAttribute())
                                ElementAttribute(nodeName, reader.Name, reader.Value);
                            if (noChilds)
                                EndElement(nodeName);
                            break;
                        case XmlNodeType.Text:
                            ElementValue(nodeName, reader.Value);
                            break;
                        case XmlNodeType.EndElement:
                            EndElement(reader.Name);
                            break;
                    }
                }
            }
            catch(Exception e) {
                XamlText = e.ToString();
            }
        }

        private static System.IO.MemoryStream GenerateStreamFromString(string value)
        {
            return new System.IO.MemoryStream(Encoding.UTF8.GetBytes(value ?? ""));
        }

        private Path currentPath;

        private void NewElement(string name) 
        {
            if (name == "path")
            {
                currentPath = new Path();
            }
        }
        private void EndElement(string name) 
        {
            if (name == "path")
            {
                Canvas.Children.Add(currentPath);

                double left = (Canvas.ActualWidth - currentPath.ActualWidth) / 2;
                Canvas.SetLeft(currentPath, left);

                double top = (Canvas.ActualHeight - currentPath.ActualHeight) / 2;
                Canvas.SetTop(currentPath, top);


                GeometryFormatter formatter = new GeometryFormatter();

                XamlText = string.Format("{0}\n<Path Fill=\"{1}\">\n\t<Path.Data>\n\t\t<PathGeometry FillRule=\"NonZero\" Figures=\"{2}\"/>\n\t</Path.Data>\n</Path>", 
                    XamlText,
                    currentPath.Fill,
                    currentPath.Data.ToString(formatter));
            }
        }
        private void ElementAttribute(string name, string attribue, string value)
        {
            IFormatProvider format  = System.Globalization.CultureInfo.InvariantCulture;

            if (name == "svg")
            {
                if (attribue == "viewBox")
                {
                    var parts = value.Split(' ', ',');
                    viewport = new Rect(
                        double.Parse(parts[0], format),
                        double.Parse(parts[1], format),
                        double.Parse(parts[2], format),
                        double.Parse(parts[3], format));

                    Canvas.Children.Clear();
                }
            }

            if (name == "path")
            {
                if (attribue == "d")
                {
                    value = value
                        .Replace("M", "\nM ").Replace("m", "\nm ")
                        .Replace("L", "\nL ").Replace("l", "\nl ")
                        .Replace("H", "\nH ").Replace("h", "\nh ")
                        .Replace("V", "\nV ").Replace("v", "\nv ")
                        .Replace("V", "\nV ").Replace("v", "\nv ")
                        .Replace("C", "\nC ").Replace("c", "\nc ")
                        .Replace("Q", "\nQ ").Replace("q", "\nq ")
                        .Replace("S", "\nS ").Replace("s", "\ns ")
                        .Replace("T", "\nT ").Replace("t", "\nt ")
                        .Replace("A", "\nA ").Replace("a", "\na ")
                        .Replace("z", "\nz");

                    var parts = value.Split(new char[]{'\n'}, StringSplitOptions.RemoveEmptyEntries);

                    for (int i = 0; i < parts.Length; i++)
                    {
                        parts[i] = parts[i].Replace("-", " -");
                        var subparts = parts[i].Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);

                        if (subparts[0] == "M" || subparts[0] == "m" ||
                            subparts[0] == "L" || subparts[0] == "l")
                        {
                            subparts[1] = TransformDouble(subparts[1], viewport.Width, format);
                            subparts[2] = TransformDouble(subparts[2], viewport.Height, format);
                        }
                        else if (subparts[0] == "S" || subparts[0] == "s")
                        {
                            subparts[1] = TransformDouble(subparts[1], viewport.Width, format);
                            subparts[2] = TransformDouble(subparts[2], viewport.Height, format);
                            subparts[3] = TransformDouble(subparts[3], viewport.Width, format);
                            subparts[4] = TransformDouble(subparts[4], viewport.Height, format);
                        }
                        else if (subparts[0] == "C" || subparts[0] == "c")
                        {
                            subparts[1] = TransformDouble(subparts[1], viewport.Width, format);
                            subparts[2] = TransformDouble(subparts[2], viewport.Height, format);
                            subparts[3] = TransformDouble(subparts[3], viewport.Width, format);
                            subparts[4] = TransformDouble(subparts[4], viewport.Height, format);
                            subparts[5] = TransformDouble(subparts[5], viewport.Width, format);
                            subparts[6] = TransformDouble(subparts[6], viewport.Height, format);
                        }
                        else if (subparts[0] == "H" || subparts[0] == "h")
                        {
                            subparts[1] = TransformDouble(subparts[1], viewport.Width, format);
                        }
                        else if (subparts[0] == "V" || subparts[0] == "v")
                        {
                            subparts[1] = TransformDouble(subparts[1], viewport.Height, format);
                        }
                        else if (subparts[0] == "z") { }
                        else
                        {
                            throw new Exception("Uknown path point: " + subparts[0]);
                        }

                        parts[i] = string.Join(" ", subparts);
                    }

                    value = string.Join(" ", parts);

                    currentPath.Data = Geometry.Parse(value);
                }

                if (attribue == "fill")
                {
                    TypeConverter converter = new ColorConverter();
                    currentPath.Fill = new SolidColorBrush((Color)converter.ConvertFromString(value));
                }
            }
        }
        private void ElementValue(string name, string value) { }

        private string TransformDouble(string value, double size, IFormatProvider format)
        {
            return (double.Parse(value, format) / size).ToString("F5", format);
        }

        void OnPropertyChanged(string prop)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
