using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls.Primitives;
using SKYPE4COMLib;

namespace SkyPhotoSharing
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public MainWindow()
        {
            InitializeComponent();
        }

        #region イベント

        private void ViewWindow_OnInitialized(object sender, EventArgs e)
        {
            ViewWindow.Cursor = HandOpenCursor;
            Thumbnails.ItemContainerGenerator.ItemsChanged += OnThumbnailsUpdated;
        }

        private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Configuration.Instance.Save();
            if (EnlisterList != null) EnlisterList.Close();
            if (PhotoList != null) PhotoList.Close();

        }

        #region 画像ファイルドラッグ&ドロップ

        private void OnPreviewDragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.None;
            if (e.Data.GetData(DataFormats.FileDrop) != null)
            {
                e.Effects = DragDropEffects.Copy;
            }
            e.Handled = true;
        }

        private void OnDrop(object sender, DragEventArgs e)
        {
            try
            {
                string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
                foreach (string p in files)
                {
                    log.Debug(p);
                    if (Photo.IsCoinsidable(p))
                    {
                        PhotoList.AddNewLocal(p);
                    }
                }
            }
            catch (ApplicationException ex)
            {
                MessageBox.Show(this, ex.Message, string.Format(Properties.Resources.MESSAGE_ERROR_OCCURRED_CAPTION, ex.GetType().ToString()));
            }

        }

        #endregion

        #region サムネイル操作

        #region 画像選択時のスクロール位置決定

        private void OnThumbnailSelect(object sender, SelectionChangedEventArgs e)
        {
            PhotoList.UpdateControlFlags();
            Thumbnails.ScrollIntoView(Thumbnails.Items.CurrentItem);
        }

        #endregion

        #region 画像を閉じた後のカレント画像変更

        private void OnThumbnailClose(object sender, RoutedEventArgs e)
        {
            int i = Thumbnails.Items.CurrentPosition;
            RemovePhoto();

            if (Thumbnails.Items.Count <= i)
            {
                Thumbnails.Items.MoveCurrentToLast();
            }
            else if (0 < i)
            {
                Thumbnails.Items.MoveCurrentTo(Thumbnails.Items.GetItemAt(i));
            }
            else
            {
                Thumbnails.Items.MoveCurrentToFirst();
            }
        }

        #endregion

        #region 画像が増えた場合の新画像へのカレント移動

        private int _preUpdateCount = 0;
        void OnThumbnailsUpdated(object sender, ItemsChangedEventArgs e)
        {
            if (_preUpdateCount < Thumbnails.Items.Count) Thumbnails.Items.MoveCurrentToLast();
            _preUpdateCount = Thumbnails.Items.Count;
        }

        #endregion

        #endregion

        #region オンラインユーザー追加

        private void OnShowOnlineUsers(object sender, RoutedEventArgs e)
        {
            AddMemberButton.ContextMenu.IsOpen = true;
        }

        private void OnShowOnlineUsers(object sender, ContextMenuEventArgs e)
        {
            SelectableUsers.Reflesh();
        }

        void OnAddUser(object sender, RoutedEventArgs e)
        {
            MenuItem m = e.Source as MenuItem;
            User u = m.DataContext as User;
            Enlisters.Instance.ConnectTo(u);
        }

        #endregion

        #region View操作

        #region View更新

        private void View_SourceChanged(object sender, DataTransferEventArgs e)
        {
            ResetViewTransforms(Thumbnails.SelectedItem as Photo);
        }

        #endregion

        #region  Viewのマウスホイールでの拡大/縮小

        private void View_OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScaleTransform sc = ((TransformGroup)ViewMap.LayoutTransform).Children.ElementAt(0) as ScaleTransform;
            Point omp = e.GetPosition(ViewMap); 
            ScaleChangeByWheel(sc, e.Delta); 
            CorrectScrollAtMouse(e.GetPosition(ViewWindow), omp, sc.ScaleX);
        }

        #endregion
        
        #region Viewのマウスドラッグでのスクロール
 
        private void View_OnScrollEnter(object sender, MouseButtonEventArgs e)
        {
            ScrollStartPoint = e.GetPosition(ViewWindow);
            ViewWindow.Cursor = HandGrabCursor;
        }

        private void View_OnScrollOver(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) ScrollToPointedPosition(e.GetPosition(ViewWindow));
        }

        private void View_OnScrollLeave(object sender, MouseButtonEventArgs e)
        {
            ViewWindow.Cursor = HandOpenCursor;
        }

        private void View_OnScrolled(object sender, ScrollChangedEventArgs e)
        {
            UpdatePhotoViewPosition(Thumbnails.SelectedItem as Photo);
        }

        #endregion

        #region コントロールボタンによる操作

        private void OnSavePhoto(object sender, RoutedEventArgs e)
        {
            var p = Thumbnails.SelectedItem as Photo;
            p.Save();
            PhotoList.UpdateControlFlags();
        }

        private void OnScaleOrigin(object sender, RoutedEventArgs e)
        {
            Photo p = Thumbnails.Items.CurrentItem as Photo;
            p.Scale = 1.0;
        }

        private void OnScaleToFit(object sender, RoutedEventArgs e)
        {
            Photo p = Thumbnails.Items.CurrentItem as Photo;
            p.Scale = CalcFitScale(p);
        }

        #endregion

        #endregion

        #endregion

        #region リソース

        private Cursor HandOpenCursor
        {
            get { return ((TextBlock)Resources["HandOpenCursor"]).Cursor; }
        }

        private Cursor HandGrabCursor
        {
            get { return ((TextBlock)Resources["HandGrabCursor"]).Cursor; }
        }

        private PhotoList PhotoList
        {
            get { return DataContext as PhotoList; }
        }

        private SelectableUsers SelectableUsers
        {
            get { return AddMemberButton.ContextMenu.DataContext as SelectableUsers; } 
        }

        private SharingEnlisters EnlisterList
        {
            get { return ShareList.DataContext as SharingEnlisters; }
        }

        #endregion

        private const double ZOOM_DELTA = 0.05;

        private void ScaleChangeByWheel(ScaleTransform scale, double delta)
        {
            double v = ZoomDelta(delta);
            double ns = scale.ScaleX + v;
            if (ns > ZOOM_DELTA) {
                scale.ScaleX = ns;
                scale.ScaleY = ns;
            } else {
                scale.ScaleX = ZOOM_DELTA;
                scale.ScaleY = ZOOM_DELTA;
            }
        }
        
        private void CorrectScrollAtMouse(Point viewPos, Point mapPos, double mapScale)
        {
            double x = mapPos.X * mapScale - viewPos.X;
            double y = mapPos.Y * mapScale - viewPos.Y;
            ViewWindow.ScrollToHorizontalOffset(x);
            ViewWindow.ScrollToVerticalOffset(y);
        }

        private double ZoomDelta(double delta)
        {
            return delta > 0 ? ZOOM_DELTA : -ZOOM_DELTA;
        }

        private Point ScrollStartPoint { set; get; }

        private void ScrollToPointedPosition(Point delta)
        {
            double x = ViewWindow.HorizontalOffset + (ScrollStartPoint.X - delta.X);
            double y = ViewWindow.VerticalOffset + (ScrollStartPoint.Y - delta.Y);
            ViewWindow.ScrollToHorizontalOffset(x);
            ViewWindow.ScrollToVerticalOffset(y);
            ScrollStartPoint = delta;
        }

        private void ResetViewTransforms(Photo p) 
        {
            if (p == null) return;
            ViewWindow.ScrollToHorizontalOffset(p.ViewPosition.X);
            ViewWindow.ScrollToVerticalOffset(p.ViewPosition.Y);
        }
 
        private void UpdatePhotoViewPosition(Photo p)
        {
            if (p == null) return;
            p.ViewPosition = new Point(ViewWindow.HorizontalOffset, ViewWindow.VerticalOffset);
        }

        private void RemovePhoto()
        {
            Photo p = Thumbnails.Items.CurrentItem as Photo;
            PhotoList.Remove(p);
        }

        private double CalcFitScale(Photo p)
        {
            var xs = ViewWindow.ActualWidth / p.Image.PixelWidth;
            var ys = ViewWindow.ActualHeight / p.Image.PixelHeight;
            var ns = xs <= ys ? xs : ys;
            if (ns > 1.0)
            {
                ns = 1.0;
            }
            return ns;
        }

    }
}
