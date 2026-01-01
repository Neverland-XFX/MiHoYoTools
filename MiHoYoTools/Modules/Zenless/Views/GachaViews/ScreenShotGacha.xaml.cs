// Copyright (c) 2021-2024, JamXi JSG-LLC.
// All rights reserved.

// This file is part of MiHoYoTools.Modules.Zenless.

// ZenlessTools is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// ZenlessTools is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.

// You should have received a copy of the GNU General Public License
// along with MiHoYoTools.Modules.Zenless.  If not, see <http://www.gnu.org/licenses/>.

// For more information, please refer to <https://www.gnu.org/licenses/gpl-3.0.html>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.ApplicationModel;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using MiHoYoTools.Modules.Zenless.Depend;
using MiHoYoTools.Modules.Zenless.Views.ToolViews;
using static MiHoYoTools.App;
using MiHoYoTools.Core;
using MiHoYoTools.Data;

namespace MiHoYoTools.Modules.Zenless.Views.GachaViews
{
    public sealed partial class ScreenShotGacha : Page
    {
        public static bool isShowGachaRecords = false;
        public static bool isScreenShotSelf = false;
        public static bool isHideUID = true;
        public static bool isFinished = false;
        public static string FilePath = null;
        public TaskCompletionSource<bool> TaskCompletionSource { get; set; }
        public Window CurrentWindow { get; set; }

        public ScreenShotGacha()
        {
            this.InitializeComponent();
            Logging.Write("Switch to ScreenShotGacha", 0);
            LoadData();
            if (isShowGachaRecords)
            {
                GachaRecords_Viewer.Visibility = Visibility.Visible;
            }
            else
            {
                GachaRecords_Viewer.Visibility = Visibility.Collapsed;
                if (TempGachaGrid.ColumnDefinitions.Count > 0)
                {
                    TempGachaGrid.ColumnDefinitions.RemoveAt(TempGachaGrid.ColumnDefinitions.Count - 1);
                }
            }
        }

        private async void LoadData()
        {
            Logging.Write("Starting LoadData method", 0);
            string selectedUID = GachaView.selectedUid;
            int selectedCardPoolId = GachaView.selectedCardPoolId;
            Logging.Write($"Selected UID: {selectedUID}, Selected Card Pool ID: {selectedCardPoolId}", 0);

            app_name.Text = AppInfoHelper.GetDisplayName();
            app_version.Text = AppInfoHelper.GetVersionString();

            var gachaData = GachaRepository.GetZenlessGachaData(selectedUID);
            if (gachaData?.list == null || gachaData.list.Count == 0)
            {
                Logging.Write("No gacha records found for UID: " + selectedUID, 1);
                Console.WriteLine("未找到UID的调频记录");
                return;
            }

            var cardPoolInfo = GachaView.cardPoolInfo ?? new GachaModel.CardPoolInfo { CardPools = new List<GachaModel.CardPool>() };
            if (cardPoolInfo.CardPools == null)
            {
                cardPoolInfo.CardPools = new List<GachaModel.CardPool>();
            }
            var records = gachaData.list.Where(pool => pool.cardPoolId == selectedCardPoolId).SelectMany(pool => pool.records).ToList();
            Logging.Write($"Total records found: {records.Count}", 0);

            // ɸѡ�����Ǻ����ǵļ�¼
            var rank4Records = records.Where(r => r.rankType == "3").ToList();
            var rank5Records = records.Where(r => r.rankType == "4").ToList();
            Logging.Write($"4-star records count: {rank4Records.Count}, 5-star records count: {rank5Records.Count}", 0);

            // �����ƽ��з��鲢����ÿ�������еļ�¼����
            var rank4Grouped = rank4Records.GroupBy(r => r.name).Select(g => new GachaModel.GroupedRecord { name = g.Key, count = g.Count() }).ToList();
            var rank5Grouped = rank5Records.GroupBy(r => r.name).Select(g => new GachaModel.GroupedRecord { name = g.Key, count = g.Count() }).ToList();
            Logging.Write("Grouped records by name", 0);

            // ��ʾ��¼����
            Task displayGachaDetailsTask = DisplayGachaDetails(gachaData, rank4Records, rank5Records, selectedCardPoolId, cardPoolInfo);

            // ��ʾ�鿨����
            Task displayGachaInfoTask = DisplayGachaInfo(records, selectedCardPoolId, cardPoolInfo);

            // ��ʾ��Ƶ��¼
            Task displayGachaRecordsTask = DisplayGachaRecords(records);

            // �ȴ������������
            await Task.WhenAll(displayGachaDetailsTask, displayGachaInfoTask, displayGachaRecordsTask);

            Logging.Write("LoadData method finished", 0);
            await Task.Delay(1000);
            if (isScreenShotSelf) 
            { 
                await CaptureScreenshotAsync(this.Content);
            }
        }

        public void CloseWindow()
        {
            isFinished = true;
            TaskCompletionSource?.SetResult(isScreenShotSelf);
            CurrentWindow?.Close(); // ʹ�ñ���Ĵ���ʵ���رմ���
        }

        private string MaskUID(string uid)
        {
            if (uid.Length < 1) return uid; // ��ֹUID���Ȳ���

            char lastChar = uid[uid.Length - 1];
            return new string('*', uid.Length - 1) + lastChar;
        }


        public async Task CaptureScreenshotAsync(UIElement element)
        {
            try
            {
                // ��Ⱦ UIElement �� RenderTargetBitmap
                var renderTargetBitmap = new RenderTargetBitmap();
                await renderTargetBitmap.RenderAsync(element);

                // ��ȡ��������
                var pixelBuffer = await renderTargetBitmap.GetPixelsAsync();

                // ʹ�� DataReader �� IBuffer ת��Ϊ�ֽ�����
                byte[] pixels;
                using (var reader = DataReader.FromBuffer(pixelBuffer))
                {
                    pixels = new byte[pixelBuffer.Length];
                    reader.ReadBytes(pixels);
                }

                // ��ȡ�ĵ��ļ���·�����������ļ���
                string gachaScreenshotsFolderPath = AppPaths.ResolveGameFolder(GameType.ZenlessZoneZero, "GachaScreenshots");

                // �������ļ���
                Directory.CreateDirectory(gachaScreenshotsFolderPath);

                var now = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
                // �����ļ�·��
                string filePath = Path.Combine(gachaScreenshotsFolderPath, "GachaScreenShot_" + GachaView.selectedUid + "_" + now + ".png");

                // ʹ�� System.IO �����ļ���������������
                using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, fileStream.AsRandomAccessStream());
                    encoder.SetPixelData(
                        BitmapPixelFormat.Bgra8,
                        BitmapAlphaMode.Ignore,
                        (uint)renderTargetBitmap.PixelWidth,
                        (uint)renderTargetBitmap.PixelHeight,
                        96, 96,
                        pixels
                    );

                    await encoder.FlushAsync();
                }
                SharedDatas.ScreenShotData.ScreenShotPath = filePath;
                isFinished = true;
                CloseWindow();
            }
            catch (Exception ex)
            {
                DialogManager.RaiseDialog(XamlRoot, "", "");
                var dialog = new ContentDialog
                {
                    Title = "错误",
                    Content = $"截图时发生错误: {ex.Message}",
                    CloseButtonText = "确定"
                };

                await dialog.ShowAsync();
            }
        }

        private async Task DisplayGachaRecords(List<GachaModel.GachaRecord> records)
        {
            Logging.Write("Displaying gacha records", 0);
            GachaRecords_List.ItemsSource = records;
        }

        private async Task DisplayGachaInfo(List<GachaModel.GachaRecord> records, int selectedCardPoolId, GachaModel.CardPoolInfo cardPoolInfo)
        {
            Logging.Write("Displaying gacha info", 0);
            var selectedCardPool = cardPoolInfo.CardPools?.FirstOrDefault(cp => cp.CardPoolId == selectedCardPoolId);

            var rank5Records = records.Where(r => r.rankType == "4")
                                       .Select(r => new
                                       {
                                           r.name,
                                           Count = CalculateCount(records, r.id, "4"),
                                           Pity = CalculatePity(records, r.name, "4", selectedCardPoolId, cardPoolInfo),
                                           PityVisibility = Visibility.Collapsed
                                       }).ToList();

            if (rank5Records.Count == 0) GachaInfo_List_Disable.Visibility = Visibility.Visible;

            GachaInfo_List.ItemsSource = rank5Records;
            Logging.Write("Finished displaying gacha info", 0);
        }

        private string CalculateCount(List<GachaModel.GachaRecord> records, string id, string rankType)
        {
            Logging.Write("Calculating count since last target star", 0);
            int countSinceLastTargetStar = 1;
            bool foundTargetStar = false;
            for (int i = records.Count - 1; i >= 0; i--)
            {
                var record = records[i];
                if (record.rankType == rankType && record.id == id)
                {
                    foundTargetStar = true;
                    break;
                }
                if (record.rankType == "4")
                {
                    countSinceLastTargetStar = 1;
                }
                else
                {
                    countSinceLastTargetStar++;
                }
            }
            if (!foundTargetStar)
            {
                return "未找到";
            }

            Logging.Write($"Count since last target star: {countSinceLastTargetStar}", 0);
            return $"{countSinceLastTargetStar}";
        }


        private string CalculatePity(List<GachaModel.GachaRecord> records, string name, string rankType, int selectedCardPoolId, GachaModel.CardPoolInfo cardPoolInfo)
        {
            Logging.Write("Calculating pity", 0);
            var selectedCardPool = cardPoolInfo?.CardPools?.FirstOrDefault(cp => cp.CardPoolId == selectedCardPoolId);
            if (selectedCardPool == null)
            {
                return "";
            }
            var specialNames = new List<string> { "猫又", "11号", "柯蕾妲", "格莉丝", "丽娜", "莱卡恩" };

            if (specialNames.Contains(name))
            {
                if (selectedCardPool.isPityEnable == false) return "";
                Logging.Write("Pity result: 歪了", 0);
                return "歪了";
            }
            else
            {
                Logging.Write("Pity result: 未歪", 0);
                return "";
            }
        }

        private List<int> CalculateIntervals(List<GachaModel.GachaRecord> records, string rankType)
        {
            var intervals = new List<int>();
            int countSinceLastStar = 0;

            // ���������¼
            foreach (var record in records.AsEnumerable().Reverse())
            {
                countSinceLastStar++; // ÿ�ε���������������

                if (record.rankType == rankType)
                {
                    intervals.Add(countSinceLastStar); // ����������ֵ���ӵ�����б���
                    countSinceLastStar = 0; // ���ü�����
                }
            }

            return intervals;
        }

        private async Task DisplayGachaDetails(GachaModel.GachaData gachaData, List<GachaModel.GachaRecord> rank4Records, List<GachaModel.GachaRecord> rank5Records, int selectedCardPoolId, GachaModel.CardPoolInfo cardPoolInfo)
        {
            Logging.Write("Displaying gacha details", 0);
            if (cardPoolInfo == null)
            {
                cardPoolInfo = new GachaModel.CardPoolInfo { CardPools = new List<GachaModel.CardPool>() };
            }
            else if (cardPoolInfo.CardPools == null)
            {
                cardPoolInfo.CardPools = new List<GachaModel.CardPool>();
            }
            Gacha_Panel.Children.Clear();
            var scrollView = new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden,
                VerticalScrollBarVisibility = ScrollBarVisibility.Hidden
            };

            var contentPanel = new StackPanel();

            var selectedRecords = gachaData.list
                .Where(pool => pool.cardPoolId == selectedCardPoolId)
                .SelectMany(pool => pool.records)
                .OrderByDescending(r => r.time)
                .ToList();

            Logging.Write($"Total selected records: {selectedRecords.Count}", 0);

            int countSinceLast5Star = 0;
            int countSinceLast4Star = 0;
            bool foundLast5Star = false;
            bool foundLast4Star = false;

            foreach (var record in selectedRecords)
            {
                if (!foundLast5Star && record.rankType == "4")
                {
                    foundLast5Star = true;
                    foundLast4Star = true;
                }
                else if (!foundLast5Star)
                {
                    countSinceLast5Star++;
                }

                if (!foundLast4Star && record.rankType == "3")
                {
                    foundLast4Star = true;
                }
                else if (!foundLast4Star)
                {
                    countSinceLast4Star++;
                }

                if (foundLast5Star && foundLast4Star)
                {
                    break;
                }
            }


            // �������Ǻ����ǵļ��
            var fourStarIntervals = CalculateIntervals(selectedRecords, "3");
            var fiveStarIntervals = CalculateIntervals(selectedRecords, "4");

            // ����ƽ��ֵ
            string averageDraws4Star = fourStarIntervals.Count > 0 ? (fourStarIntervals.Average()).ToString("F2") : "无";
            string averageDraws5Star = fiveStarIntervals.Count > 0 ? (fiveStarIntervals.Average()).ToString("F2") : "无";

            if (isHideUID) Gacha_UID.Text = MaskUID(gachaData.info.uid);
            else Gacha_UID.Text = gachaData.info.uid;
            GachaRecords_Count.Text = $"共{selectedRecords.Count()}抽";
            GachaInfo_SinceLast5Star.Text = $"距上次S级已{countSinceLast5Star}抽";

            var basicInfoPanel = CreateDetailBorder();
            var stackPanelBasicInfo = new StackPanel();
            if (isHideUID)
                stackPanelBasicInfo.Children.Add(new TextBlock { Text = $"UID: {MaskUID(gachaData.info.uid)}", FontWeight = FontWeights.Bold });
            else
                stackPanelBasicInfo.Children.Add(new TextBlock { Text = $"UID: {gachaData.info.uid}", FontWeight = FontWeights.Bold });

            stackPanelBasicInfo.Children.Add(new TextBlock { Text = $"累计调频: {selectedRecords.Count}" });
            stackPanelBasicInfo.Children.Add(new TextBlock { Text = $"抽到S级数: {rank5Records.Count}" });
            stackPanelBasicInfo.Children.Add(new TextBlock { Text = $"抽到A级数: {rank4Records.Count}" });
            stackPanelBasicInfo.Children.Add(new TextBlock { Text = $"预计消耗: {selectedRecords.Count * 160}" });
            basicInfoPanel.Child = stackPanelBasicInfo;
            contentPanel.Children.Add(basicInfoPanel);

            var detailInfoPanel = CreateDetailBorder();
            var stackPanelDetailInfo = new StackPanel();
            stackPanelDetailInfo.Children.Add(new TextBlock { Text = "详细统计", FontWeight = FontWeights.Bold });

            stackPanelDetailInfo.Children.Add(new TextBlock { Text = $"S级平均抽数: {averageDraws5Star}抽" });
            stackPanelDetailInfo.Children.Add(new TextBlock { Text = $"A级平均抽数: {averageDraws4Star}抽" });

            string rate4Star = rank4Records.Count > 0 ? (rank4Records.Count / (double)selectedRecords.Count * 100).ToString("F2") + "%" : "无";
            string rate5Star = rank5Records.Count > 0 ? (rank5Records.Count / (double)selectedRecords.Count * 100).ToString("F2") + "%" : "无";

            stackPanelDetailInfo.Children.Add(new TextBlock { Text = $"S级获取率: {rate5Star}" });
            stackPanelDetailInfo.Children.Add(new TextBlock { Text = $"A级获取率: {rate4Star}" });

            if (rank5Records.Any())
            {
                stackPanelDetailInfo.Children.Add(new TextBlock { Text = $"最近S级: {rank5Records.First().time}" });
            }
            else
            {
                stackPanelDetailInfo.Children.Add(new TextBlock { Text = "最近S级: 无" });
            }

            if (rank4Records.Any())
            {
                stackPanelDetailInfo.Children.Add(new TextBlock { Text = $"最近A级: {rank4Records.First().time}" });
            }
            else
            {
                stackPanelDetailInfo.Children.Add(new TextBlock { Text = "最近A级: 无" });
            }

            detailInfoPanel.Child = stackPanelDetailInfo;
            contentPanel.Children.Add(detailInfoPanel);

            // �������ǵ������Ƭ
            var borderFiveStar = CreateDetailBorder();
            var stackPanelFiveStar = new StackPanel();
            stackPanelFiveStar.Children.Add(new TextBlock { Text = $"距上次S级已抽{countSinceLast5Star}抽", FontWeight = FontWeights.Bold });

            var selectedCardPool = cardPoolInfo.CardPools.FirstOrDefault(cp => cp.CardPoolId == selectedCardPoolId);
            if (selectedCardPool != null && selectedCardPool.FiveStarPity.HasValue)
            {
                var progressBar5 = CreateProgressBar(countSinceLast5Star, selectedCardPool.FiveStarPity.Value);
                stackPanelFiveStar.Children.Add(progressBar5);
                stackPanelFiveStar.Children.Add(new TextBlock { Text = $"保底{selectedCardPool.FiveStarPity.Value}抽", FontSize = 12, Foreground = new SolidColorBrush(Colors.Gray) });
            }
            borderFiveStar.Child = stackPanelFiveStar;
            contentPanel.Children.Add(borderFiveStar);

            // �������ǵ������Ƭ
            var borderFourStar = CreateDetailBorder();
            var stackPanelFourStar = new StackPanel();
            stackPanelFourStar.Children.Add(new TextBlock { Text = $"距上次A级已抽{countSinceLast4Star}抽", FontWeight = FontWeights.Bold });

            if (selectedCardPool != null && selectedCardPool.FourStarPity.HasValue)
            {
                var progressBar4 = CreateProgressBar(countSinceLast4Star, selectedCardPool.FourStarPity.Value);
                stackPanelFourStar.Children.Add(progressBar4);
                stackPanelFourStar.Children.Add(new TextBlock { Text = $"保底{selectedCardPool.FourStarPity.Value}抽", FontSize = 12, Foreground = new SolidColorBrush(Colors.Gray) });
            }
            borderFourStar.Child = stackPanelFourStar;
            contentPanel.Children.Add(borderFourStar);

            scrollView.Content = contentPanel;
            Gacha_Panel.Children.Add(scrollView);
            Logging.Write("Finished displaying gacha details", 0);
        }

        private Border CreateDetailBorder()
        {
            return new Border
            {
                Padding = new Thickness(10),
                Margin = new Thickness(0, 4, 0, 4),
                BorderBrush = new SolidColorBrush(Colors.Gray),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8)
            };
        }

        private ProgressBar CreateProgressBar(int value, int maximum)
        {
            return new ProgressBar
            {
                Minimum = 0,
                Maximum = maximum,
                Value = value,
                Height = 12
            };
        }
    }
}


