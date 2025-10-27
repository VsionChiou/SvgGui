using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using SharpVectors.Converters;

namespace SvgGui
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow:INotifyPropertyChanged
    {
        private Point? _lastDragPosition;
        private const double ZoomFactor = 1.1; // 缩放因子
        private Point _initialCenter;
        
        // 放置图标标志位
        private bool _cancelSelect = true;
        private bool _placeIcon = false;

        // 编辑模式
        private bool _isEditMode = false;
        private bool _dragDrop = false;
        private Border? _dragIcon = null;
        
        // 拖动相关变量
        private Point _dragStartPosition;      // 拖动开始时鼠标位置
        private UIElement _draggedElement;     // 当前被拖动的元素
        private Point _elementStartPosition;   // 拖动开始时元素的位置

        public static readonly DependencyProperty SourceUriProperty = DependencyProperty.Register(
            nameof(SourceUri), typeof(Uri), typeof(MainWindow), new PropertyMetadata(default(Uri)));

        public bool IsEditMode
        {
            get => _isEditMode;
            set
            {
                _isEditMode = value;
                OnPropertyChanged();
            }
        }
        public Uri SourceUri
        {
            get => (Uri)GetValue(SourceUriProperty);
            set
            {
                SetValue(SourceUriProperty, value); 
                OnPropertyChanged();
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            
            MainGrid.DataContext = this;
            TopAreaGrid.DataContext = this;

        }
        
        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            // 创建文件选择对话框
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                // 设置文件筛选器，只显示SVG文件
                Filter = "SVG Files (*.svg)|*.svg|All Files (*.*)|*.*",
                Title = "选择SVG图像文件"
            };

            // 显示对话框，如果用户选择了文件并点击了确定
            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    // 加载并显示选中的SVG文件
                    DisplaySvgFile(openFileDialog.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"加载SVG文件时出错: {ex.Message}", "错误", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        
        private void DisplaySvgFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                MessageBox.Show("指定的SVG文件不存在！", "提示", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                SvgViewbox.Source = new Uri(filePath);
            }
            catch (Exception ex)
            {
                throw new Exception($"SVG转换失败: {ex.Message}");
            }
        }

        private void Layer1_Click(object sender, RoutedEventArgs e)
        {
            Canvas1.Visibility = Canvas1.Visibility == Visibility.Visible ? Visibility.Hidden : Visibility.Visible;
            Layer1Btn.Foreground = Canvas1.Visibility == Visibility.Visible
                ? new SolidColorBrush(Colors.Black)
                : new SolidColorBrush(Colors.Gray);

            Layer1Btn.Background = Canvas1.Visibility == Visibility.Visible 
                ? new SolidColorBrush(Colors.Red) 
                : new SolidColorBrush(Colors.WhiteSmoke);
        }

        private void Layer2_Click(object sender, RoutedEventArgs e)
        {
            Canvas2.Visibility = Canvas2.Visibility == Visibility.Visible ? Visibility.Hidden : Visibility.Visible;
            Layer2Btn.Foreground = Canvas2.Visibility == Visibility.Visible
                ? new SolidColorBrush(Colors.Black)
                : new SolidColorBrush(Colors.Gray);
            
            Layer2Btn.Background = Canvas2.Visibility == Visibility.Visible 
                ? new SolidColorBrush(Colors.Red) 
                : new SolidColorBrush(Colors.WhiteSmoke);
        }

        private void Layer3_Click(object sender, RoutedEventArgs e)
        {
            Canvas3.Visibility = Canvas3.Visibility == Visibility.Visible ? Visibility.Hidden : Visibility.Visible;
            Layer3Btn.Foreground = Canvas3.Visibility == Visibility.Visible
                ? new SolidColorBrush(Colors.Black)
                : new SolidColorBrush(Colors.Gray);
            
            Layer3Btn.Background = Canvas3.Visibility == Visibility.Visible 
                ? new SolidColorBrush(Colors.Red) 
                : new SolidColorBrush(Colors.WhiteSmoke);
        }

        private void SvgBorderMouse_Wheel(object sender, MouseWheelEventArgs e)
        {
            double zoom = e.Delta > 0 ? ZoomFactor : 1 / ZoomFactor;

            // 获取相对于SVG的鼠标位置
            Point mousePos = e.GetPosition(MainCanvas);

            // 计算当前缩放中心
            double centerX = mousePos.X * MainScale.ScaleX + MainTranslate.X;
            double centerY = mousePos.Y * MainScale.ScaleY + MainTranslate.Y;

            // 应用新缩放
            MainScale.ScaleX *= zoom;
            MainScale.ScaleY *= zoom;

            // 调整位置保持鼠标点不变
            MainTranslate.X = centerX - mousePos.X * MainScale.ScaleX;
            MainTranslate.Y = centerY - mousePos.Y * MainScale.ScaleY;
        }

        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Right)
            {
                _lastDragPosition = e.GetPosition(MainGrid);
                MainGrid.CaptureMouse();
            }
            else if (e.ChangedButton == MouseButton.Left)
            {
                
                _lastDragPosition = e.GetPosition(MainGrid);
                MainGrid.CaptureMouse();
                
                if (_placeIcon)
                {
                    Point currentPosition = e.GetPosition(MainGrid);
                    
                    var confirmedIcon = new SvgViewbox
                    {
                        Source = SourceUri,
                        Width = 12,
                        Height = 12,
                        Stretch = System.Windows.Media.Stretch.Uniform
                    };

                    // confirmedIcon.MouseEnter += Icon_MouseEnter;
                    // confirmedIcon.MouseLeave += Icon_MouseLeave;

                    Border iconBorder = new Border
                    {
                        Child = confirmedIcon,
                        BorderBrush = Brushes.Transparent,
                        Width = 14,
                        Height = 14
                    };

                    iconBorder.MouseEnter += Icon_MouseEnter;
                    iconBorder.MouseLeave += Icon_MouseLeave;
                    iconBorder.MouseLeftButtonDown += Icon_MouseLeftButtonDown;
                    iconBorder.MouseLeftButtonUp += Icon_MouseLeftButtonUp;
                    iconBorder.MouseMove += Icon_MouseMove;
                    
                    Point relativePosition = ConvertToRelativePosition(currentPosition);
                    Canvas.SetLeft(iconBorder, relativePosition.X - 6);
                    Canvas.SetTop(iconBorder, relativePosition.Y - 6);

                    // 添加到Canvas2图层
                    Canvas2.Children.Add(iconBorder);

                    _placeIcon = false;
                    _cancelSelect = true;

                    FreeStyleSvgViewbox.Visibility = Visibility.Collapsed;
                    Canvas.SetLeft(FreeStyleSvgViewbox, 0);
                    Canvas.SetTop(FreeStyleSvgViewbox, 0);
                }

                foreach (var border in Canvas2.Children.OfType<Border>())
                {
                    border.BorderBrush = Brushes.Transparent;
                }
            }
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_lastDragPosition.HasValue && !_placeIcon) return;

            Point currentPos = e.GetPosition(MainGrid);

            // 右键放置
            if (_lastDragPosition.HasValue)
            {
                if (currentPos.X - _lastDragPosition.Value.X > 2 || currentPos.Y - _lastDragPosition.Value.Y > 2)
                {
                    _cancelSelect = false;
                }

                MainTranslate.X += currentPos.X - _lastDragPosition.Value.X;
                MainTranslate.Y += currentPos.Y - _lastDragPosition.Value.Y;
                _lastDragPosition = currentPos;
            }

            if (_placeIcon)
            {
                var generalTransform = MainGrid.TransformToVisual(Canvas2).Inverse;
                if (generalTransform != null)
                {
                    Point relativePos = ConvertToRelativePosition(currentPos);
                    Canvas.SetLeft(FreeStyleSvgViewbox, relativePos.X - 6);
                    Canvas.SetTop(FreeStyleSvgViewbox, relativePos.Y - 6);
                }
            }

            if (_dragDrop&&_isEditMode&&_dragIcon!=null)
            {
                // 计算鼠标移动距离
                double deltaX = currentPos.X - _dragStartPosition.X;
                double deltaY = currentPos.Y - _dragStartPosition.Y;
        
                // 计算新位置
                double newLeft = _elementStartPosition.X + deltaX;
                double newTop = _elementStartPosition.Y + deltaY;
        
                // 设置新位置
                Canvas.SetLeft(_dragIcon, newLeft);
                Canvas.SetTop(_dragIcon, newTop);
            }
        }
        
        private Point ConvertToRelativePosition(Point absolutePos)
        {
            if (MainCanvas.RenderTransform is TransformGroup transformGroup && transformGroup.Value.HasInverse)
            {
                // 计算逆变换
                Matrix matrix = transformGroup.Value;
                matrix.Invert();
                return matrix.Transform(absolutePos);
            }
        
            // 无法逆变换时返回原始位置
            return absolutePos;
        }

        private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Right)
            {
                _lastDragPosition = null;
                MainGrid.ReleaseMouseCapture();
                
                if (_cancelSelect)
                {
                    MainGrid.Cursor = Cursors.Arrow;
                    FreeStyleSvgViewbox.Visibility = Visibility.Collapsed;
                    _placeIcon = false;
                }
                else _cancelSelect = true;
            }else if (e.ChangedButton==MouseButton.Left)
            {
                _lastDragPosition = null;
                MainGrid.ReleaseMouseCapture();
                
                if (_cancelSelect)
                {
                    MainGrid.Cursor = Cursors.Arrow;
                    FreeStyleSvgViewbox.Visibility = Visibility.Collapsed;
                    _placeIcon = false;
                }
                else _cancelSelect = true;
                
                if (_cancelSelect)
                {
                    MainGrid.Cursor = Cursors.Arrow;
                }
            }
        }
        
        // 鼠标进入图标（悬停）
        private void Icon_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Border icon)
            {
                icon.Cursor = Cursors.Hand;
            }
        }

        // 鼠标离开图标（结束悬停）
        private void Icon_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Border icon)
            {
                icon.Cursor = Cursors.Arrow;
            }
        }
        
        private void Icon_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border iconBorder)
            {
                e.Handled = true;
                
                foreach (var item in Canvas2.Children.OfType<Border>())
                {
                    item.BorderBrush = Brushes.Transparent;
                }
                iconBorder.BorderBrush = Brushes.Blue;
                iconBorder.BorderThickness = new Thickness(1);
                
                _dragIcon = iconBorder;
                
                _dragStartPosition = e.GetPosition(MainGrid);
                
                _elementStartPosition = new Point(
                    Canvas.GetLeft(iconBorder),
                    Canvas.GetTop(iconBorder)
                );
                
                iconBorder.CaptureMouse();
            }

            if (IsEditMode)
            {
                _dragDrop = true;
            }
        }
        
        private void Icon_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border iconBorder)
            {
                // e.Handled = true;
                _dragIcon = null;
                iconBorder.ReleaseMouseCapture();
            }
            _dragDrop = false;
        }
        
        private void Icon_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_dragDrop || _dragIcon == null || !IsEditMode) return;
            
            Point currentPos = e.GetPosition(MainGrid);
            var generalTransform = MainGrid.TransformToVisual(Canvas2).Inverse;
            if (generalTransform != null)
            {
                Point relativePos = ConvertToRelativePosition(currentPos);
                Canvas.SetLeft(_dragIcon, relativePos.X - 6);
                Canvas.SetTop(_dragIcon, relativePos.Y - 6);
            }
            
        }

        private void AlertBtn_Click(object sender, RoutedEventArgs e)
        {
            BaseIconBtnClick("pack://application:,,,/SvgGui;component/Resources/Icons/alarm.svg");
        }
        
        private void PlugBtn_Click(object sender, RoutedEventArgs e)
        {
            BaseIconBtnClick("pack://application:,,,/SvgGui;component/Resources/Icons/bolt-one.svg");
        }
        
        private void ChargingTreasureBtn_Click(object sender, RoutedEventArgs e)
        {
            BaseIconBtnClick("pack://application:,,,/SvgGui;component/Resources/Icons/charging-treasure.svg");
        }

        private void DeskLampBtn_Click(object sender, RoutedEventArgs e)
        {
            BaseIconBtnClick("pack://application:,,,/SvgGui;component/Resources/Icons/desk-lamp.svg");
        }

        private void LampBtn_Click(object sender, RoutedEventArgs e)
        {
            BaseIconBtnClick("pack://application:,,,/SvgGui;component/Resources/Icons/lamp.svg");
        }

        private void StackLightBtn_Click(object sender, RoutedEventArgs e)
        {
            BaseIconBtnClick("pack://application:,,,/SvgGui;component/Resources/Icons/stack-light.svg");
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        private void BaseIconBtnClick(string uri)
        {
            _placeIcon = true;
            SourceUri = new Uri(uri);
            MainGrid.Cursor = Cursors.Cross;
            FreeStyleSvgViewbox.Visibility = Visibility.Visible;
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            SvgViewbox.Width = MainGrid.ActualWidth;
            SvgViewbox.Height = MainGrid.ActualHeight;
        }
    }
}