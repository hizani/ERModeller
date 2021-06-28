using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ERMODELLER
{
    public class ZoomBehavior : DependencyObject
    {
        /// <summary>
        /// Поле IsEnabled подключаемого элемента
        /// </summary>
        public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
         "IsEnabled", typeof(bool), typeof(ZoomBehavior), new PropertyMetadata(default(bool), ZoomBehavior.OnIsEnabledChanged));

        /// <summary>
        /// Сеттер IsEnabled
        /// </summary>
        /// <param name="attachingElement">Прикрепляемый объект</param>
        /// <param name="value">Устанавливаемое свойству значение</param>
        public static void SetIsEnabled(DependencyObject attachingElement, bool value) => attachingElement.SetValue(ZoomBehavior.IsEnabledProperty, value);

        /// <summary>
        /// Геттер IsEnabled
        /// </summary>
        /// <param name="attachingElement">Прикрепляемый объект</param>
        /// <returns>Значение IsEnabled</returns>
        public static bool GetIsEnabled(DependencyObject attachingElement) => (bool)attachingElement.GetValue(ZoomBehavior.IsEnabledProperty);

        /// <summary>
        /// Поле ZoomFactorProperty подключаемого элемента
        /// </summary>
        public static readonly DependencyProperty ZoomFactorProperty = DependencyProperty.RegisterAttached(
          "ZoomFactor", typeof(double), typeof(ZoomBehavior), new PropertyMetadata(0.1));

        /// <summary>
        /// Сеттер ZoomFactorProperty
        /// </summary>
        /// <param name="attachingElement">Прикрепляемый объект</param>
        /// <param name="value">Устанавливаемое свойству значение</param>
        public static void SetZoomFactor(DependencyObject attachingElement, double value) => attachingElement.SetValue(ZoomBehavior.ZoomFactorProperty, value);

        /// <summary>
        /// Геттер ZoomFactorProperty
        /// </summary>
        /// <param name="attachingElement">Прикрепляемый объект</param>
        /// <returns>Значение ZoomFactorProperty</returns>
        public static double GetZoomFactor(DependencyObject attachingElement) => (double)attachingElement.GetValue(ZoomBehavior.ZoomFactorProperty);

        /// <summary>
        /// Поле ScrollViewerProperty подключаемого элемента
        /// </summary>
        public static readonly DependencyProperty ScrollViewerProperty = DependencyProperty.RegisterAttached(
          "ScrollViewer", typeof(ScrollViewer), typeof(ZoomBehavior), new PropertyMetadata(default(ScrollViewer)));

        /// <summary>
        /// Сеттер ScrollViewerProperty
        /// </summary>
        /// <param name="attachingElement">Прикрепляемый объект</param>
        /// <param name="value">Устанавливаемое свойству значение</param>
        public static void SetScrollViewer(DependencyObject attachingElement, ScrollViewer value) => attachingElement.SetValue(ZoomBehavior.ScrollViewerProperty, value);

        /// <summary>
        /// Геттер ScrollViewerProperty
        /// </summary>
        /// <param name="attachingElement">Прикрепляемый объект</param>
        /// <returns>Значение ScrollViewerProperty</returns>
        public static ScrollViewer GetScrollViewer(DependencyObject attachingElement) => (ScrollViewer)attachingElement.GetValue(ZoomBehavior.ScrollViewerProperty);

        private static void OnIsEnabledChanged(DependencyObject attachingElement, DependencyPropertyChangedEventArgs e)
        {
            if (!(attachingElement is FrameworkElement frameworkElement))
            {
                throw new ArgumentException("Прикрепляемый элемент должен обладать типом FrameworkElement");
            }

            bool isEnabled = (bool)e.NewValue;
            if (isEnabled)
            {
                frameworkElement.PreviewMouseWheel += ZoomBehavior.Zoom_OnMouseWheel;
                if (ZoomBehavior.TryGetScaleTransform(frameworkElement, out _))
                {
                    return;
                }

                if (frameworkElement.LayoutTransform is TransformGroup transformGroup)
                {
                    transformGroup.Children.Add(new ScaleTransform());
                }
                else
                {
                    frameworkElement.LayoutTransform = new ScaleTransform();
                }
            }
            else
            {
                frameworkElement.PreviewMouseWheel -= ZoomBehavior.Zoom_OnMouseWheel;
            }
        }

        private static void Zoom_OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var zoomTargetElement = sender as FrameworkElement;

            Point mouseCanvasPosition = e.GetPosition(zoomTargetElement);
            double scaleFactor = e.Delta > 0
              ? ZoomBehavior.GetZoomFactor(zoomTargetElement)
              : -1 * ZoomBehavior.GetZoomFactor(zoomTargetElement);

            ZoomBehavior.ApplyZoomToAttachedElement(mouseCanvasPosition, scaleFactor, zoomTargetElement);

            ZoomBehavior.AdjustScrollViewer(mouseCanvasPosition, scaleFactor, zoomTargetElement);
        }

        /// <summary>
        /// Метод применения изменения парметра
        /// ScaleTransform к целевому объекту
        /// </summary>
        /// <param name="mouseCanvasPosition">Позиция мыши на холсте</param>
        /// <param name="scaleFactor">уровень приближения\отдаления</param>
        /// <param name="zoomTargetElement">Целевой ассоциированный элемент</param>
        private static void ApplyZoomToAttachedElement(Point mouseCanvasPosition, double scaleFactor, FrameworkElement zoomTargetElement)
        {
            if (!ZoomBehavior.TryGetScaleTransform(zoomTargetElement, out ScaleTransform scaleTransform))
            {
                throw new InvalidOperationException("ScaleTransform не найден");
            }

            scaleTransform.CenterX = mouseCanvasPosition.X;
            scaleTransform.CenterY = mouseCanvasPosition.Y;

            scaleTransform.ScaleX = Math.Max(0.1, scaleTransform.ScaleX + scaleFactor);
            scaleTransform.ScaleY = Math.Max(0.1, scaleTransform.ScaleY + scaleFactor);
        }

        /// <summary>
        /// Метод ассоциации объекта ZoomBehavior с целевым объектом ScrollViewer
        /// </summary>
        /// <param name="mouseCanvasPosition">Позиция мыши на холсте</param>
        /// <param name="scaleFactor">уровень приближения\отдаления</param>
        /// <param name="zoomTargetElement">Целевой ассоциированный элемент</param>
        private static void AdjustScrollViewer(Point mouseCanvasPosition, double scaleFactor, FrameworkElement zoomTargetElement)
        {
            ScrollViewer scrollViewer = ZoomBehavior.GetScrollViewer(zoomTargetElement);
            if (scrollViewer == null)
            {
                return;
            }

            scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset + mouseCanvasPosition.X * scaleFactor);
            scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset + mouseCanvasPosition.Y * scaleFactor);
        }

        /// <summary>
        /// Метод получения параметра scaleTransform
        /// </summary>
        /// <param name="frameworkElement">Ассоциированный элемент</param>
        /// <param name="scaleTransform">Возвращаемый параметр scaleTransform</param>
        /// <returns></returns>
        private static bool TryGetScaleTransform(FrameworkElement frameworkElement, out ScaleTransform scaleTransform)
        {
            switch (frameworkElement.LayoutTransform)
            {
                case TransformGroup transformGroup:
                    scaleTransform = transformGroup.Children.OfType<ScaleTransform>().FirstOrDefault();
                    break;

                case ScaleTransform transform:
                    scaleTransform = transform;
                    break;

                default:
                    scaleTransform = null;
                    break;
            }

            return scaleTransform != null;
        }
    }
}