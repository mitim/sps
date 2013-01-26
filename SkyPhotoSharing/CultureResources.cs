using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

namespace SkyPhotoSharing
{
    public class CultureResources
    {
        static readonly List<CultureInfo> cultures = new List<CultureInfo>();

        static CultureResources()
        {
            // 言語の選択候補として日本語(日本)と英語(米国)を追加する
            cultures.Add(CultureInfo.GetCultureInfo("ja-JP"));
            cultures.Add(CultureInfo.GetCultureInfo("en-US"));
        }

        public static IList<CultureInfo> Cultures
        {
            get { return cultures; }
        }

        public Properties.Resources GetResourceInstance()
        {
            // resxファイルから自動生成されたクラスのインスタンスを返す
            return new Properties.Resources();
        }

        private static ObjectDataProvider provider;
        public static ObjectDataProvider ResourceProvider
        {
            get
            {
                // キー「ResourcesInstance」はApp.xaml内で定義している
                if (provider == null && System.Windows.Application.Current != null)
                    provider = (ObjectDataProvider)System.Windows.Application.Current.FindResource("ResourcesInstance");
                return provider;
            }
        }

        public static void ChangeCulture(CultureInfo culture)
        {
            // 言語の切り替えメソッド
            Properties.Resources.Culture = culture;
            ResourceProvider.Refresh();
        }
    }
}