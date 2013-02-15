using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls.Primitives;
using SKYPE4COMLib;
using System.Threading;
using System.IO;
using System.Collections.Generic;

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
            Effector = new ImageEffector();
        }

        #region イベント

        private void ViewWindow_OnInitialized(object sender, EventArgs e)
        {
            ViewWindow.Cursor = HandOpenCursor;
            Thumbnails.ItemContainerGenerator.ItemsChanged += OnThumbnailsUpdated;
            SkypeConnection.Instance.SetEventOnOnRecievePostcard(SkypePostcard.RAISE_SELECT_FILE, OnThumbnailSelectByOtherUser);
            SkypeConnection.Instance.EventOnTransferExceptionOccurred += OnTransferExceptionOccurred;
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
            Activate();
            try
            {
                ForceCursor = true;
                Cursor = Cursors.Wait;
                string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
                AddFiles(files);
            }
            catch (ApplicationException ex)
            {
                MessageBox.Show(this, ex.Message, string.Format(Properties.Resources.MESSAGE_ERROR_OCCURRED_CAPTION, ex.GetType().ToString()));
            }
            finally
            {
                ForceCursor = false;
                Cursor = null;
            }

        }

        #endregion

        #region サムネイル操作


        #region 画像選択時のシェアリング

        private void OnThumbnailClick(object sender, RoutedEventArgs e)
        {
            if (Configuration.Instance.AutoSelect != false) return;
            SkypeConnection.Instance.BloadcastPostcard(SkypePostcard.CreateRaiseSelectFileCard(Thumbnails.Items.CurrentItem as Photo));
            e.Handled = true;
        }

        private void OnNothingWork(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
        }

        #endregion

        #region 画像選択時のスクロール位置決定(シェアユーザによる選択を含む)

        private void OnThumbnailSelect(object sender, SelectionChangedEventArgs e)
        {
            PhotoList.UpdateControlFlags();
            Thumbnails.ScrollIntoView(Thumbnails.Items.CurrentItem);
        }

        private void OnThumbnailSelectByOtherUser(SkypePostcard card)
        {
            var p = PhotoList.GetSameItem(card.Message as SkypeMessageUniqueFile);
            if (p == null) return;
            Thumbnails.Items.MoveCurrentTo(p);
            Thumbnails.ScrollIntoView(p);
        }

        #endregion

        #region 画像を閉じた後のカレント画像変更

        private void OnThumbnailClose(object sender, RoutedEventArgs e)
        {
            int i = Thumbnails.Items.CurrentPosition;
            RemovePhoto();
            e.Handled = true;

            if (Thumbnails.Items.Count <= i)
            {
                Thumbnails.Items.MoveCurrentToLast();
            }
            else if (0 < i)
            {
                log.Debug(Thumbnails.Items.GetItemAt(i).GetType().ToString());
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
            if (_preUpdateCount < Thumbnails.Items.Count)
            {
                PhotoList.Last.IsSelected = true;
                Thumbnails.Items.MoveCurrentToLast();
            }
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
            var p = Thumbnails.SelectedItem as Photo;
            if (p == null) return;
            ResetViewTransforms(p);
        }

        #endregion

        #region  Viewのマウスホイールでの拡大/縮小

        private void View_OnScaleChange(object sender, MouseWheelEventArgs e)
        {
            var p = Thumbnails.SelectedItem as Photo;
            if (p == null) return;
            Point omp = e.GetPosition(ViewMap);
            Effector.Scale(p, e.Delta);
            Effector.CorrectScaledOffset(p, e.GetPosition(ViewWindow), omp, ViewWindowRect);
            ScrollToViewWindowOffsets(p);
        }

        #endregion

        private void View_OnMouseOver(object sender, MouseEventArgs e)
        {
            switch (Effector.Mode)
            {
                case ImageEffector.EffectMode.SCROLL:
                    if (e.LeftButton != MouseButtonState.Pressed) return;
                    View_OnScrollOver(sender, e);
                    break;
                case ImageEffector.EffectMode.ROTATE:
                    if (e.MiddleButton != MouseButtonState.Pressed) return;
                    View_OnRotateOver(sender, e);
                    break;
            }
        }

        #region Viewのマウスドラッグでのスクロール

        private void View_OnScrollEnter(object sender, MouseButtonEventArgs e)
        {
            Effector.PrevScroll(e.GetPosition(ViewWindow));
            ViewWindow.Cursor = HandGrabCursor;
        }



        private void View_OnScrollOver(object sender, MouseEventArgs e)
        {
            var p = Thumbnails.SelectedItem as Photo;
            if (p == null) return;
            Effector.ScrollTo(p, e.GetPosition(ViewWindow), ViewWindowRect);
            ScrollToViewWindowOffsets(p);
        }

        private void View_OnScrollLeave(object sender, MouseButtonEventArgs e)
        {
            ViewWindow.Cursor = HandOpenCursor;
        }

        private void View_OnScrolled(object sender, ScrollChangedEventArgs e)
        {
            var p = Thumbnails.SelectedItem as Photo;
            if (p == null) return;
            p.WindowPosition = ViewWindowOffset;
        }

        #endregion

        #region Viewの中ボタンによる回転

        private void View_OnRotateEnter(object sender, MouseEventArgs e)
        {
            var p = Thumbnails.SelectedItem as Photo;
            if (p == null) return;
            if (e.MiddleButton != MouseButtonState.Pressed) return;
            Effector.PrevRotate(e.GetPosition(ViewMap));
            ViewWindow.Cursor = HandGrabCursor;
            ShowCenterScope(p);
        }

        private void View_OnRotateOver(object sender, MouseEventArgs e)
        {
            var p = Thumbnails.SelectedItem as Photo;
            if (p == null) return;
            Effector.RotateTo(p, ViewMapRect, e.GetPosition(ViewMap), ViewWindowRect);
            var rtf = ((RotateTransform)((TransformGroup)ViewImage.RenderTransform).Children[0]);
            log.Debug("rotate:" + rtf.Angle + " center:" + rtf.CenterX + "," + rtf.CenterY);
            log.Debug("scroll:" + ViewWindow.HorizontalOffset + "," + ViewWindow.VerticalOffset + " image:" + Canvas.GetTop(ViewImage) + "," + Canvas.GetLeft(ViewImage) + "," + ViewImage.ActualWidth + "," + ViewImage.ActualHeight);
        }

        private void View_OnRotateLeave(object sender, MouseButtonEventArgs e)
        {
            HideCenterScope();
            ViewWindow.Cursor = HandOpenCursor;
        }


        #endregion

        #region コントロールボタンによる操作

        private void OnSavePhoto(object sender, RoutedEventArgs e)
        {
            var p = Thumbnails.SelectedItem as Photo;
            p.Save();
            PhotoList.UpdateControlFlags();
        }

        private void OnOpenDialog(object sender, RoutedEventArgs e)
        {
            try
            {
                var files = OpenPhotoFileDialog();
                ForceCursor = true;
                Cursor = Cursors.Wait;
                AddFiles(files);
            }
            catch (ApplicationException ex)
            {
                MessageBox.Show(this, ex.Message, string.Format(Properties.Resources.MESSAGE_ERROR_OCCURRED_CAPTION, ex.GetType().ToString()));
            }
            finally
            {
                ForceCursor = false;
                Cursor = null;
            }
        }

        private void OnScaleOrigin(object sender, RoutedEventArgs e)
        {
            Photo p = Thumbnails.Items.CurrentItem as Photo;
            if (p == null) return;
            Effector.ScaleToOriginalSize(p, ViewWindowRect);
            ScrollToViewWindowOffsets(p);
        }

        private void OnScaleToFit(object sender, RoutedEventArgs e)
        {
            Photo p = Thumbnails.Items.CurrentItem as Photo;
            if (p == null) return;
            Effector.ScaleToFitSize(p, ViewWindowRect);
            ScrollToViewWindowOffsets(p);
        }

        private void OnRotateTo0(object sender, RoutedEventArgs e)
        {
            Photo p = Thumbnails.Items.CurrentItem as Photo;
            if (p == null) return;
            Effector.RotateTo(p, 0, ViewWindowRect);
        }

        private void OnRotateTo90(object sender, RoutedEventArgs e)
        {
            Photo p = Thumbnails.Items.CurrentItem as Photo;
            if (p == null) return;
            Effector.RotateTo(p, 90, ViewWindowRect);
        }

        private void OnRotateTo180(object sender, RoutedEventArgs e)
        {
            Photo p = Thumbnails.Items.CurrentItem as Photo;
            if (p == null) return;
            Effector.RotateTo(p, 180, ViewWindowRect);
        }

        private void OnRotateTo270(object sender, RoutedEventArgs e)
        {
            Photo p = Thumbnails.Items.CurrentItem as Photo;
            if (p == null) return;
            Effector.RotateTo(p, 270, ViewWindowRect);
        }

        private void OnFlipHorizontal(object sender, RoutedEventArgs e)
        {
            Photo p = Thumbnails.Items.CurrentItem as Photo;
            if (p == null) return;
            Effector.FlipHorizontal(p);
        }

        private void OnFlipVertical(object sender, RoutedEventArgs e)
        {
            Photo p = Thumbnails.Items.CurrentItem as Photo;
            if (p == null) return;
            Effector.FlipVertical(p);
        }

        #endregion

        #endregion

        #region メッセージボックス表示

        private void OnTransferExceptionOccurred(FileTransactionException ex)
        {
            MessageBox.Show(this, ex.Message, string.Format(Properties.Resources.MESSAGE_ERROR_OCCURRED_CAPTION, ex.GetType().ToString()));
        }

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

        private BindablePhotoList PhotoList
        {
            get { return DataContext as BindablePhotoList; }
        }

        private BindableFriens SelectableUsers
        {
            get { return AddMemberButton.ContextMenu.DataContext as BindableFriens; }
        }

        private BindableEnlisters EnlisterList
        {
            get { return ShareList.DataContext as BindableEnlisters; }
        }

        private Point ViewWindowOffset
        {
            get
            {
                return new Point(ViewWindow.HorizontalOffset, ViewWindow.VerticalOffset);
            }
        }

        private Size ViewWindowSize
        {
            get
            {
                return new Size(ViewWindow.ActualWidth, ViewWindow.ActualHeight);
            }
        }

        private Rect ViewMapRect
        {
            get
            {
                return new Rect(0, 0, ViewMap.ActualWidth, ViewMap.ActualHeight);
            }
        }

        private Rect ViewWindowRect
        {
            get
            {
                return new Rect(
                    ViewWindow.HorizontalOffset,
                    ViewWindow.VerticalOffset,
                    ViewWindow.ActualWidth,
                    ViewWindow.ActualHeight
                );
            }
        }

        #endregion

        private ImageEffector Effector { get; set; }

        private void AddFiles(string[] files)
        {
            foreach (string p in files)
            {
                log.Debug(p);
                try
                {
                    if (Photo.IsCoinsidable(p))
                    {
                        PhotoList.AddNewLocal(p);
                        Thread.Sleep(100);
                    }
                }
                catch (NotSupportedException ex)
                {
                    log.Error(ex.Message, ex);
                    MessageBox.Show(this, string.Format(Properties.Resources.ERROR_FILE_NOT_PHOTO, Path.GetFileName(p)), string.Format(Properties.Resources.MESSAGE_ERROR_OCCURRED_CAPTION, ex.GetType().ToString()));
                    continue;
                }
            }
            Thumbnails.Items.MoveCurrentToLast();
            Thumbnails.ScrollIntoView(Thumbnails.Items.CurrentItem);
        }

        private void ResetViewTransforms(Photo p)
        {
            if (p == null) return;
            SetVMapPoint(p);

            Effector.InitializeViewPos(
                p,
                new Vector(ViewWindow.ActualWidth, ViewWindow.ActualHeight),
                new Vector(p.MapSpan, p.MapSpan)
            );
            ScrollToViewWindowOffsets(p);
        }

        private void SetVMapPoint(Photo p)
        {

            Canvas.SetLeft(ViewImage, p.MapLeft);
            Canvas.SetTop(ViewImage, p.MapTop);
        }

        private void ScrollToViewWindowOffsets(Photo p)
        {
            ViewWindow.ScrollToHorizontalOffset(p.WindowPosition.X);
            ViewWindow.ScrollToVerticalOffset(p.WindowPosition.Y);
        }

        private void ShowCenterScope(Photo p)
        {
            Canvas.SetLeft(CenterScope, (p.MapLeft + p.CenterX) - 5);
            Canvas.SetTop(CenterScope, (p.MapTop + p.CenterY) - 5);
            CenterScope.Visibility = Visibility.Visible;
        }

        private void HideCenterScope()
        {
            CenterScope.Visibility = Visibility.Hidden;
        }

        private void RemovePhoto()
        {
            Photo p = Thumbnails.Items.CurrentItem as Photo;
            PhotoList.Remove(p);
        }

        private static string[] OpenPhotoFileDialog()
        {
            var od = new System.Windows.Forms.OpenFileDialog();
            od.Multiselect = true;
            od.CheckFileExists = true;
            od.CheckPathExists = true;
            od.Filter = Photo.SupportFilter;
            od.DefaultExt = Photo.SUPPORT_EXT[0];
            od.Title = Properties.Resources.TITLE_FILE_OPEN;
            od.InitialDirectory = Configuration.Instance.SaveFolder;
            var r = od.ShowDialog();
            if (r != System.Windows.Forms.DialogResult.OK) return new string[0];
            return od.FileNames;
        }
    }
}
