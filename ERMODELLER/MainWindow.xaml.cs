using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ERMODELLER
{
    /// <summary>
    /// Класс главного рабочего пространства
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// поле перетаскиваемого объекта
        /// </summary>
        private UIElement dragObject = null;

        /// <summary>
        /// поле смещения точки от указанной координаты
        /// </summary>
        private Point offset;

        /// <summary>
        /// путь к файлу проекта
        /// </summary>
        public static string projPath = "";

        public MainWindow()
        {
            InitializeComponent();
            Frame_CreatePageLoader.Content = new CreateTablePage();
            Frame_EditPageLoader.Content = new EditTablePage();
            Loaded += delegate
            {
                RedrawArrows();
            };
            if (projPath != "")
            {
                Console.WriteLine(projPath);
                CreateFromEr(projPath);
            }
        }

        /// <summary>
        /// Метод добавления табилцы
        /// </summary>
        /// <param name="coord">Точка появления таблицы</param>
        /// <param name="tableName">Имя таблицы</param>
        /// <param name="attrs">Аттрибуты таблицы</param>
        /// <param name="connections">Связи</param>
        /// <returns>Созданная табилца</returns>
        public int AddTable(Point coord, string tableName, List<DbTableItem> attrs, List<DbTableModel> connections)
        {
            DbTableModel dbTableData = new DbTableModel { TableName = tableName, Attributes = attrs, Connections = connections, X = coord.X, Y = coord.Y };
            DbTable dbTable = new DbTable(dbTableData);
            Canvas.SetLeft(dbTable, coord.X);
            Canvas.SetTop(dbTable, coord.Y);
            dbTable.PreviewMouseLeftButtonDown += UserCTRL_PreviewMouseDown;
            return CanvasMain.Children.Add(dbTable);
        }

        /// <summary>
        /// Метод определения ключевых точек для построения стрелки
        /// </summary>
        /// <param name="p1">Координата первой таблицы</param>
        /// <param name="p2">Координата второй таблицы</param>
        /// <param name="startTable">Начальная таблица</param>
        /// <param name="endTable">Конечная таблица</param>
        public void DefinePivots(ref Point p1, ref Point p2, DbTableModel startTable, DbTableModel endTable)
        {
            bool isAbove = p1.Y < p2.Y;

            bool isBetween = (p1.Y < p2.Y && p1.Y + startTable.Height > p2.Y + endTable.Height) ||
                (
                (p1.Y + startTable.Height >= p2.Y && p1.Y + startTable.Height <= p2.Y + endTable.Height) ||
                (p1.Y >= p2.Y && p1.Y <= p2.Y + endTable.Height)
                );

            bool isLeft = p1.X + startTable.Width <= p2.X && isBetween;

            bool isRight = p1.X >= p2.X + endTable.Width && isBetween;
            switch (isAbove)
            {
                case true:
                    if (isLeft)
                    {
                        p1 = new Point(p1.X + startTable.Width, p1.Y + endTable.Height / 2);
                        p2 = new Point(p2.X, p2.Y + endTable.Height / 2);
                        return;
                    }
                    if (isRight)
                    {
                        p1 = new Point(p1.X, p1.Y + endTable.Height / 2);
                        p2 = new Point(p2.X + endTable.Width, p2.Y + endTable.Height / 2);
                        return;
                    }
                    p1 = new Point(p1.X + startTable.Width / 2, p1.Y + startTable.Height);
                    p2 = new Point(p2.X + endTable.Width / 2, p2.Y);
                    return;

                case false:
                    if (isLeft)
                    {
                        p1 = new Point(p1.X + startTable.Width, p1.Y + endTable.Height / 2);
                        p2 = new Point(p2.X, p2.Y + endTable.Height / 2);
                        return;
                    }
                    if (isRight)
                    {
                        p1 = new Point(p1.X, p1.Y + endTable.Height / 2);
                        p2 = new Point(p2.X + endTable.Width, p2.Y + endTable.Height / 2);
                        return;
                    }
                    p1 = new Point(p1.X + startTable.Width / 2, p1.Y);
                    p2 = new Point(p2.X + endTable.Width / 2, p2.Y + endTable.Height);
                    return;
            }
        }

        /// <summary>
        /// Метод удаления табилцы
        /// </summary>
        /// <param name="delTablData">Модель удаляемой табилцы</param>
        /// <param name="delTable">Удаляемая таблица</param>
        public void DeleteTable(DbTableModel delTablData, DbTable delTable)
        {
            try
            {
                for (int i = 0; i < CanvasMain.Children.Count; i++)
                {
                    if (CanvasMain.Children[i] is DbTable)
                        if (((DbTable)CanvasMain.Children[i]).TableModel.Connections.Contains(delTablData))
                            throw new Exception("На эту таблицу имеются ссылки");
                }
                CanvasMain.Children.Remove(delTable);
                RedrawArrows();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Метод перерисовки стрелок на холесте
        /// </summary>
        public void RedrawArrows()
        {
            for (int i = 0; i < CanvasMain.Children.Count; i++)
            {
                if (CanvasMain.Children[i] is Shape)
                {
                    CanvasMain.Children.RemoveAt(i);
                    i--;
                }
            }
            var children = CanvasMain.Children.OfType<DbTable>().ToList();
            foreach (var child in children)
            {
                child.TableModel.Height = child.ActualHeight;
                child.TableModel.Width = child.ActualWidth;
            }
            foreach (DbTable dbTable in children)
            {
                for (int i = 0; i < dbTable.TableModel.Connections.Count; i++)
                {
                    var connectedTable = dbTable.TableModel.Connections[i];
                    var p1 = new Point((double)dbTable.GetValue(Canvas.LeftProperty), (double)dbTable.GetValue(Canvas.TopProperty));
                    var p2 = new Point(connectedTable.X, connectedTable.Y);
                    DefinePivots(ref p1, ref p2, dbTable.TableModel, connectedTable);
                    CanvasMain.Children.Add(DrawLinkArrow(p1, p2, dbTable.TableModel, connectedTable));
                }
            }
        }

        /// <summary>
        /// Сохранение модели в файл ранее указанный файл проекта
        /// </summary>
        public void SaveCurrentModelFile()
        {
            try
            {
                if (projPath == "")
                {
                    SaveModelFile();
                    return;
                }

                var tables = CanvasMain.Children.OfType<DbTable>().ToList();
                var tablesData = new List<DbTableModel>();
                for (int i = 0; i < tables.Count(); i++)
                    tablesData.Add(tables[i].TableModel);

                string json = JsonConvert.SerializeObject(tablesData, new JsonSerializerSettings { PreserveReferencesHandling = PreserveReferencesHandling.Objects });

                using (StreamWriter sw = File.CreateText(projPath))
                {
                    sw.WriteLine(json);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Сохранение модели в новый файл проекта
        /// </summary>
        public void SaveModelFile()
        {
            try
            {
                var tables = CanvasMain.Children.OfType<DbTable>().ToList();
                var tablesData = new List<DbTableModel>();
                for (int i = 0; i < tables.Count(); i++)
                    tablesData.Add(tables[i].TableModel);

                string json = JsonConvert.SerializeObject(tablesData, new JsonSerializerSettings { PreserveReferencesHandling = PreserveReferencesHandling.Objects });

                SaveFileDialog dlg = new SaveFileDialog();
                dlg.DefaultExt = ".er";
                dlg.Filter = "ER-Model (.er)|*.er";
                string filename = "";
                if (dlg.ShowDialog() != true)
                    return;
                projPath = filename = dlg.FileName;
                using (StreamWriter sw = File.CreateText(filename))
                {
                    sw.WriteLine(json);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CanvasMain_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            return;
        }

        private void CanvasMain_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (this.dragObject == null)
                return;

            var position = e.GetPosition(sender as IInputElement);
            var newPositionY = position.Y - this.offset.Y < 0 ? 0 : position.Y - this.offset.Y;
            var newPositionX = position.X - this.offset.X < 0 ? 0 : position.X - this.offset.X;

            Canvas.SetTop(this.dragObject, newPositionY);
            Canvas.SetLeft(this.dragObject, newPositionX);
            ((DbTable)this.dragObject).TableModel.X = newPositionX;
            ((DbTable)this.dragObject).TableModel.Y = newPositionY;
            ((DbTable)this.dragObject).TableModel.Width = ((DbTable)this.dragObject).ActualWidth;
            ((DbTable)this.dragObject).TableModel.Height = ((DbTable)this.dragObject).ActualHeight;
        }

        private void CanvasMain_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (this.dragObject != null)
                RedrawArrows();
            this.dragObject = null;
            this.CanvasMain.ReleaseMouseCapture();
        }

        private void ContextMenu_AddTable_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            var newTablePosition = e.GetPosition(CanvasMain);
            ShowTableOverlay(newTablePosition);
        }

        /// <summary>
        /// Рисует стрелку по двум точкам
        /// </summary>
        /// <param name="p1">Начальная точка</param>
        /// <param name="p2">Конечная точка</param>
        /// <returns>Возвращает форму стрелки </returns>
        private Shape DrawLinkArrow(Point p1, Point p2, DbTableModel startTable, DbTableModel endTable)
        {
            GeometryGroup lineGroup = new GeometryGroup();

            //Отрисовка линии
            PathGeometry PathGeometryLine = new PathGeometry();
            PathFigure pathLine = new PathFigure();
            Point control1Point = new Point();
            Point control2Point = new Point();
            if (startTable.Y + startTable.Height >= endTable.Y && startTable.Y <= endTable.Y + endTable.Height)
            {
                double middleX = ((p1.X + p2.X) / 2.0);
                double middleY = ((p1.Y + p2.Y) / 2.0);

                double control1X = middleX;
                double control1Y = middleY + (p1.Y - middleY);
                control1Point = new Point(control1X, control1Y);
                double control2X = middleX;
                double control2Y = middleY - (p1.Y - middleY);
                control2Point = new Point(control2X, control2Y);
            }
            else
            {
                double middleX = ((p1.X + p2.X) / 2.0);
                double middleY = ((p1.Y + p2.Y) / 2.0);

                double control1X = middleX + (p1.X - middleX);
                double control1Y = middleY;
                control1Point = new Point(control1X, control1Y);
                double control2X = middleX - (p1.X - middleX);
                double control2Y = middleY;
                control2Point = new Point(control2X, control2Y);
            }

            Point[] points = new Point[] { p1, control1Point, control2Point, p2 };

            PolyLineSegment curve = new PolyLineSegment(points, true);

            pathLine.StartPoint = p1;
            pathLine.IsClosed = false;
            pathLine.IsFilled = false;
            pathLine.Segments.Add(curve);
            PathGeometryLine.Figures.Add(pathLine);

            //Отрисовка стрелки
            double theta = Math.Atan2((p2.Y - control2Point.Y), (p2.X - control2Point.X)) * 180 / Math.PI;

            PathGeometry pathGeometryArrow = new PathGeometry();
            PathFigure pathArrow = new PathFigure();
            Point p = new Point(p1.X + ((p2.X - p1.X) / 1), p1.Y + ((p2.Y - p1.Y) / 1));
            pathArrow.StartPoint = p;

            Point lpoint = new Point(p.X + 6, p.Y + 15);
            Point rpoint = new Point(p.X - 6, p.Y + 15);
            LineSegment seg1 = new LineSegment();
            seg1.Point = lpoint;
            pathArrow.Segments.Add(seg1);

            LineSegment seg2 = new LineSegment();
            seg2.Point = rpoint;
            pathArrow.Segments.Add(seg2);

            LineSegment seg3 = new LineSegment();
            seg3.Point = p;
            pathArrow.Segments.Add(seg3);

            pathGeometryArrow.Figures.Add(pathArrow);
            RotateTransform transform = new RotateTransform();
            transform.Angle = theta + 90;
            transform.CenterX = p.X;
            transform.CenterY = p.Y;
            pathGeometryArrow.Transform = transform;
            lineGroup.Children.Add(pathGeometryArrow);

            lineGroup.Children.Add(PathGeometryLine);
            System.Windows.Shapes.Path path = new System.Windows.Shapes.Path();
            path.Data = lineGroup;
            path.StrokeThickness = 1;
            path.Stroke = path.Fill = Brushes.Black;

            return path;
        }

        /// <summary>
        /// Метод геренации скрипта из построенной модели
        /// </summary>
        private void GenerateScript()
        {
            var tables = CanvasMain.Children.OfType<DbTable>().ToList();
            tables.Sort((x, y) =>
    x.TableModel.Connections.Count.CompareTo(y.TableModel.Connections.Count));
            string script = "USE [master]\nGO\n\nCREATE DATABASE [DB_NAME]\nGO\n\nUSE [DB_NAME]\nGO\n";
            foreach (var table in tables)
            {
                script += $"\n{table.TableModel.GenerateCreateTableScript()}\n";
            }
            Clipboard.SetData(DataFormats.UnicodeText, (Object)script);
        }

        private void MenuItem_ClipboardScript_PreviewMouseUp(object sender, RoutedEventArgs e)
        {
            GenerateScript();
        }

        private void MenuItem_Create_Click(object sender, RoutedEventArgs e)
        {
            CanvasMain.Children.Clear();
            projPath = "";
        }

        private void MenuItem_CurentFileSave_Click(object sender, RoutedEventArgs e)
        {
            SaveCurrentModelFile();
        }

        private void MenuItem_Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void MenuItem_FileOpen_PreviewMouseUp(object sender, RoutedEventArgs e)
        {
            OpenModelFile();
        }

        private void MenuItem_FileSave_PreviewMouseUp(object sender, RoutedEventArgs e)
        {
            SaveModelFile();
        }

        private void MenuItem_Save_PreviewMouseUp(object sender, RoutedEventArgs e)
        {
            SaveModelPng();
        }

        /// <summary>
        /// Метод открытия файла проекта
        /// </summary>
        private void OpenModelFile()
        {
            try
            {
                OpenFileDialog dlg = new OpenFileDialog();
                dlg.DefaultExt = ".er";
                dlg.Filter = "ER-Model files (.er)|*.er|Sql files (.sql)|*.sql";
                if (dlg.ShowDialog() != true)
                    return;

                if (dlg.FileName.Contains(".er"))
                    CreateFromEr(dlg.FileName);
                if (dlg.FileName.Contains(".sql"))
                    CreateFromSql(dlg.FileName);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Метод построения модели из существующего t-sql скрипта
        /// </summary>
        /// <param name="fileName">путь к файлу скрипта </param>
        private void CreateFromSql(string fileName)
        {
            string line = "";
            using (StreamReader sr = File.OpenText(fileName))
            {
                line = sr.ReadToEnd().ToUpper();
            }

            line = CleanScript(line);

            //разбиваем целый скрипт на множество маленьких
            List<string> scripts = new List<string>();
            foreach (Match match in Regex.Matches(line, @"(\w+ TABLE (.|\n)*?GO)"))
            {
                scripts.Add(match.Value.Replace("\r\nGO", ""));
            }

            CanvasMain.Children.Clear();

            foreach (var tableData in SqlParser.ScriptToModel(scripts))
            {
                DbTable dbTable = new DbTable(tableData);
                Canvas.SetLeft(dbTable, dbTable.TableModel.X);
                Canvas.SetTop(dbTable, dbTable.TableModel.Y);
                dbTable.PreviewMouseLeftButtonDown += UserCTRL_PreviewMouseDown;
                CanvasMain.Children.Add(dbTable);
            }
        }

        /// <summary>
        /// Метод очистки t-sql скрипта от комментариев
        /// </summary>
        /// <param name="line">Необработанный скрипт</param>
        /// <returns></returns>
        private static string CleanScript(string line)
        {
            //удаляем комментарии
            var blockComments = @"/\*(.*?)\*/";
            var lineComments = @"//(.*?)\r?\n";

            line = Regex.Replace(line,
    blockComments + "|" + lineComments,
    me =>
    {
        if (me.Value.StartsWith("/*") || me.Value.StartsWith("//"))
            return me.Value.StartsWith("//") ? Environment.NewLine : "";
        return me.Value;
    },
    RegexOptions.Singleline);

            return line;
        }

        /// <summary>
        /// Метод построения модели из десериализованного файла проекта
        /// </summary>
        /// <param name="fileName">путь к файлу проекта</param>
        private void CreateFromEr(string fileName)
        {
            projPath = fileName;
            string json = "";
            using (StreamReader sr = File.OpenText(fileName))
            {
                string s;
                while ((s = sr.ReadLine()) != null)
                {
                    json = s;
                }
            }

            List<DbTableModel> TableDataList = JsonConvert.DeserializeObject<List<DbTableModel>>(json, new JsonSerializerSettings { PreserveReferencesHandling = PreserveReferencesHandling.Objects });

            CanvasMain.Children.Clear();

            foreach (var tableData in TableDataList)
            {
                DbTable dbTable = new DbTable(tableData);
                Canvas.SetLeft(dbTable, dbTable.TableModel.X);
                Canvas.SetTop(dbTable, dbTable.TableModel.Y);
                dbTable.PreviewMouseLeftButtonDown += UserCTRL_PreviewMouseDown;
                CanvasMain.Children.Add(dbTable);
            }
        }

        /// <summary>
        /// Сохранение картинки модели
        /// </summary>
        private void SaveModelPng()
        {
            var rect = new Rect(CanvasMain.RenderSize);

            var rtb = new RenderTargetBitmap(
                (int)rect.Width, (int)rect.Height, 96, 96, PixelFormats.Default);

            var dv = new DrawingVisual();

            using (var dc = dv.RenderOpen())
            {
                var vb = new VisualBrush
                {
                    Visual = CanvasMain,
                    Viewbox = rect,
                    ViewboxUnits = BrushMappingMode.Absolute
                };

                dc.DrawRectangle(vb, null, rect);
            }

            rtb.Render(dv);

            BitmapEncoder pngEncoder = new PngBitmapEncoder();
            pngEncoder.Frames.Add(BitmapFrame.Create(rtb));

            try
            {
                System.IO.MemoryStream ms = new System.IO.MemoryStream();

                pngEncoder.Save(ms);
                ms.Close();

                SaveFileDialog dlg = new SaveFileDialog();
                dlg.DefaultExt = ".png";
                dlg.Filter = "Image (.png)|*.png";
                string filename = "";
                if (dlg.ShowDialog() != true)
                    return;

                filename = dlg.FileName;

                System.IO.File.WriteAllBytes(filename, ms.ToArray());
            }
            catch
            {
                MessageBox.Show("Указан недействительный путь", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Метод открытия оверлея добавления таблицы
        /// </summary>
        /// <param name="newTablePos">Позиция новой таблицы</param>
        private void ShowTableOverlay(Point newTablePos)
        {
            OverlayCreateTable.Visibility = Visibility.Visible;
            List<DbTableModel> tablesData = new List<DbTableModel>();
            var tables = CanvasMain.Children.OfType<DbTable>().ToList();
            for (int i = 0; i < tables.Count(); i++)
                tablesData.Add(tables[i].TableModel);

            ((CreateTablePage)Frame_CreatePageLoader.Content).OpenPage(tablesData, null, newTablePos);
        }

        private void UserCTRL_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            this.dragObject = sender as UIElement;
            this.offset = e.GetPosition(this.CanvasMain);
            this.offset.Y -= Canvas.GetTop(this.dragObject);
            this.offset.X -= Canvas.GetLeft(this.dragObject);
            this.CanvasMain.CaptureMouse();
        }
    }
}