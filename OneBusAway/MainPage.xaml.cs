using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.System;
using Windows.Web.Http;

namespace OneBusAway
{
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            InitializeComponent();
            buttonAdd.Content = new SymbolIcon(Symbol.Add);
            buttonAddRandom.Content = new SymbolIcon(Symbol.Shuffle);
            buttonAddPreset.Content = new SymbolIcon(Symbol.List);
            radioL1.IsChecked = true;
            checkEnglish.IsChecked = true;
            for (int i = 0; i < 100; i++)
            {
                RowDefinition row = new RowDefinition();
                row.Height = GridLength.Auto;
                gridMain.RowDefinitions.Add(row);

                TEXT_ROUTE_NUMBER[i] = new TextBlock();
                TEXT_ROUTE_NUMBER[i].FontSize = 48;
                TEXT_ROUTE_NUMBER[i].Margin = new Thickness(10, 0, 50, 0);
                gridMain.Children.Add(TEXT_ROUTE_NUMBER[i]);
                Grid.SetRow(TEXT_ROUTE_NUMBER[i], i + 1);

                STACK[i] = new StackPanel();
                gridMain.Children.Add(STACK[i]);
                Grid.SetRow(STACK[i], i + 1);
                Grid.SetColumn(STACK[i], 1);

                TEXT_DESTINATION[i] = new TextBlock();
                TEXT_DESTINATION[i].FontSize = 32;
                STACK[i].Children.Add(TEXT_DESTINATION[i]);

                TEXT_SMALL_TEXT[i] = new TextBlock();
                TEXT_SMALL_TEXT[i].FontSize = 12;
                STACK[i].Children.Add(TEXT_SMALL_TEXT[i]);

                TEXT_ARRIVAL[i] = new TextBlock();
                TEXT_ARRIVAL[i].FontSize = 48;
                TEXT_ARRIVAL[i].Margin = new Thickness(50, 0, 10, 0);
                TEXT_ARRIVAL[i].HorizontalAlignment = HorizontalAlignment.Right;
                gridMain.Children.Add(TEXT_ARRIVAL[i]);
                Grid.SetRow(TEXT_ARRIVAL[i], i + 1);
                Grid.SetColumn(TEXT_ARRIVAL[i], 2);
            }
            gridMain.RowDefinitions.Add(new RowDefinition());
            timer1.Tick += TimerEvent1;
            timer1.Interval = new TimeSpan(0, 0, 0, 0, 100);
            timer1.Start();
            timer10.Tick += TimerEvent10;
            timer10.Interval = new TimeSpan(0, 0, 0, SERVER_REFRESH);
            timer10.Start();
            timerToggle.Tick += TimerEventToggle;
            timerToggle.Interval = new TimeSpan(0, 0, 0, 5);
            addStation("1_558");
        }

        private List<string> STATIONS = new List<string>();     // stations user selected
        private List<string> ALL_STATIONS = new List<string>(); // all stations for an agency
        private string[] ROUTE_NUMBER = new string[100];
        private string[] DESTINATION = new string[100];
        private string[] VEHICLE_ID = new string[100];
        private long[] ARRIVAL = new long[100];
        private int MAX_STATIONS;
        private const int SERVER_REFRESH = 10; // interval to ping the server
        private HttpClient CLIENT = new HttpClient();
        private TextBlock[] TEXT_ROUTE_NUMBER = new TextBlock[100];
        private TextBlock[] TEXT_DESTINATION = new TextBlock[100];
        private TextBlock[] TEXT_SMALL_TEXT = new TextBlock[100];
        private TextBlock[] TEXT_ARRIVAL = new TextBlock[100];
        private StackPanel[] STACK = new StackPanel[100];
        private DispatcherTimer timer1 = new DispatcherTimer();  // used to refresh all text blocks
        private DispatcherTimer timer10 = new DispatcherTimer(); // used to ping the server
        private DispatcherTimer timerToggle = new DispatcherTimer(); // used to alternate between Chinese / English text
        private bool CHINESE = false;
        private Random RANDOM = new Random(); // an instance of an rng

        // Refreshes all text blocks.
        private void TimerEvent1(object sender, object e)
        {
            for (int i = 0; i < MAX_STATIONS; i++)
            {
                TEXT_ROUTE_NUMBER[i].Text = textCheck(ROUTE_NUMBER[i]);
                TEXT_DESTINATION[i].Text = textCheck(DESTINATION[i]);
                TEXT_SMALL_TEXT[i].Text = textCheck(ARRIVAL[i], VEHICLE_ID[i]);
                TEXT_ARRIVAL[i].Text = textCheck(ARRIVAL[i]);
            }
        }

        private async void TimerEvent10(object sender, object e)
        {
            timer10.Stop();
            List<string> predicted = new List<string>();
            List<string> scheduled = new List<string>();
            List<string> routeNumber1 = new List<string>();
            List<string> routeNumber2 = new List<string>();
            List<string> destination = new List<string>();
            List<string> vehicleId = new List<string>();
            for (int i = 0; i < STATIONS.Count; i++)
            {
                string url = "http://api.pugetsound.onebusaway.org/api/where/arrivals-and-departures-for-stop/"
                + STATIONS[i] +
                ".xml?key=09ce661b-1d78-4fc4-a77b-7c55455acadb&minutesBefore=1&minutesAfter=120&&includeReferences=false";
                string webText = await websiteReader(url, true);
                List<string> arrivalChunk = XMLReader(webText, "arrivalAndDeparture", MAX_STATIONS);
                for (int j = 0; j < arrivalChunk.Count; j++)
                {
                    predicted.Add(XMLReader(arrivalChunk[j], "predictedDepartureTime", 1)[0]);
                    scheduled.Add(XMLReader(arrivalChunk[j], "scheduledDepartureTime", 1)[0]);
                    routeNumber1.Add(XMLReader(arrivalChunk[j], "routeShortName", 1)[0]);
                    routeNumber2.Add(XMLReader(arrivalChunk[j], "routeLongName", 1)[0]);
                    destination.Add(XMLReader(arrivalChunk[j], "tripHeadsign", 1)[0]);
                    vehicleId.Add(XMLReader(arrivalChunk[j], "vehicleId", 1)[0]);
                }
            }
            List<long> arrival = new List<long>();
            for (int i = 0; i < predicted.Count; i++)
            {
                long a = getLongFromString(predicted[i]);
                if (a == 0)
                    a = getLongFromString(scheduled[i]);
                arrival.Add(a);
            }
            List<int> order = createSortOrder(arrival);
            List<string> routeNumber = new List<string>();
            for (int i = 0; i < routeNumber1.Count; i++)
            {
                string s = routeNumber1[i];
                if (s == "")
                    s = routeNumber2[i];
                s = s.Replace("0E", "0X");
                s = s.Replace("1E", "1X");
                s = s.Replace("2E", "2X");
                s = s.Replace("3E", "3X");
                s = s.Replace("4E", "4X");
                s = s.Replace("5E", "5X");
                s = s.Replace("6E", "6X");
                s = s.Replace("7E", "7X");
                s = s.Replace("8E", "8X");
                s = s.Replace("9E", "9X");
                routeNumber.Add(s);
            }
            ARRIVAL = sortUsingGivenOrder(arrival, order);
            ROUTE_NUMBER = sortUsingGivenOrder(routeNumber, order);
            DESTINATION = sortUsingGivenOrder(destination, order);
            VEHICLE_ID = sortUsingGivenOrder(vehicleId, order);
            timer10.Start();
        }

        private void button_Click_Add(object sender, RoutedEventArgs e)
        {
            addStation(textBox.Text);
            textBox.Text = "";
        }

        private void button_Click_Clear(object sender, RoutedEventArgs e)
        {
            STATIONS.Clear();
            richTextBox.Blocks.Clear();
            TimerEvent10(null, null);
        }

        private async void button_Click_AddRandom(object sender, RoutedEventArgs e)
        {
            enableControls(false);
            // agencies:
            // 1 - King County Metro
            // 3 - Pierce Transit
            // 19 - Intercity Transit
            // 23 - City of Seattle (no stops)
            // 29 - Community Transit
            // 40 - Sound Transit
            // 95 - Washington State Ferries
            // 97 - Everett Transit
            // 98 - Seattle Children's Hostpital Shuttle
            // 99 - GO Transit
            // KMD - Kingcounty Marine Division (no stops)
            if (ALL_STATIONS.Count == 0)
            {
                string s = await websiteReader("http://api.pugetsound.onebusaway.org/api/where/stop-ids-for-agency/1.xml?key=09ce661b-1d78-4fc4-a77b-7c55455acadb&&includeReferences=false", true);
                s += await websiteReader("http://api.pugetsound.onebusaway.org/api/where/stop-ids-for-agency/3.xml?key=09ce661b-1d78-4fc4-a77b-7c55455acadb&&includeReferences=false", true);
                s += await websiteReader("http://api.pugetsound.onebusaway.org/api/where/stop-ids-for-agency/19.xml?key=09ce661b-1d78-4fc4-a77b-7c55455acadb&&includeReferences=false", true);
                s += await websiteReader("http://api.pugetsound.onebusaway.org/api/where/stop-ids-for-agency/29.xml?key=09ce661b-1d78-4fc4-a77b-7c55455acadb&&includeReferences=false", true);
                s += await websiteReader("http://api.pugetsound.onebusaway.org/api/where/stop-ids-for-agency/40.xml?key=09ce661b-1d78-4fc4-a77b-7c55455acadb&&includeReferences=false", true);
                s += await websiteReader("http://api.pugetsound.onebusaway.org/api/where/stop-ids-for-agency/95.xml?key=09ce661b-1d78-4fc4-a77b-7c55455acadb&&includeReferences=false", true);
                s += await websiteReader("http://api.pugetsound.onebusaway.org/api/where/stop-ids-for-agency/97.xml?key=09ce661b-1d78-4fc4-a77b-7c55455acadb&&includeReferences=false", true);
                s += await websiteReader("http://api.pugetsound.onebusaway.org/api/where/stop-ids-for-agency/98.xml?key=09ce661b-1d78-4fc4-a77b-7c55455acadb&&includeReferences=false", true);
                s += await websiteReader("http://api.pugetsound.onebusaway.org/api/where/stop-ids-for-agency/99.xml?key=09ce661b-1d78-4fc4-a77b-7c55455acadb&&includeReferences=false", true);
                ALL_STATIONS = XMLReader(s, "string");
            }
            int a = RANDOM.Next(0, ALL_STATIONS.Count - 1);
            addStation(ALL_STATIONS[a]);
        }

        private void button_Click_AddPreset1(object sender, RoutedEventArgs e)
        {
            addStation("1_16101", false, true);
            addStation("1_16102", false, true);
            addStation("1_16103", false, true);
            addStation("1_16104", false, true);
            addStation("29_2875", false, true);
            addStation("1_16106", false, true);
            addStation("29_2876", false, true);
            addStation("1_16100", false, true);
            addStation("1_16112", true, true);
        }

        private void button_Click_AddPreset2(object sender, RoutedEventArgs e)
        {
            addStation("1_41255", false, true);
            addStation("1_18210", false, true);
            addStation("1_18630", true, true);
        }

        private void button_Click_AddPreset3(object sender, RoutedEventArgs e)
        {
            addStation("1_29247", false, true);
            addStation("1_29405", false, true);
            addStation("1_25765", false, true);
            addStation("1_99604", false, true);
            addStation("3_19473", true, true);
        }

        private void button_Click_AddPreset4(object sender, RoutedEventArgs e)
        {
            addStation("1_1120", false, true);
            addStation("1_1121", false, true);
            addStation("1_700", false, true);
            addStation("1_570", false, true);
            addStation("1_430", false, true);
            addStation("1_431", false, true);
            addStation("1_433", false, true);
            addStation("1_1108", false, true);
            addStation("1_1109", false, true);
            addStation("1_1110", false, true);
            addStation("1_760", false, true);
            addStation("1_280", false, true);
            addStation("1_575", false, true);
            addStation("1_578", true, true);
        }

        private void button_Click_Refresh(object sender, RoutedEventArgs e)
        {
            TimerEvent10(null, null);
        }

        private void TimerEventToggle(object sender, object e)
        {
            CHINESE = !CHINESE;
        }

        /** Reads the raw text from a website from a given url. */
        private async Task<string> websiteReader(string url, bool forceSuccess)
        {
            string input = "", message = "";
            int i = 0;
            while (!input.Contains("<code>200</code>") && (forceSuccess || i < 5))
            {
                await Task.Delay(500);
                try
                {
                    CLIENT.DefaultRequestHeaders.IfModifiedSince = new DateTimeOffset(DateTime.Now);
                    input = await CLIENT.GetStringAsync(new Uri(url));
                    errorText.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));
                    message = "Data last updated: ";
                }
                catch (Exception)
                {
                    errorText.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    message = "Failed to update! ";
                }
                errorText.Text = message + DateTime.Now.ToString();
                i++;
            }
            return input;
        }

        /** Reads the first n occurances enclosed by the specified tag. */
        private List<string> XMLReader(string text, string tag, int n = -1)
        {
            List<string> returnList = new List<string>();
            int i = 0;
            while (text.Contains("<" + tag + ">") && text.Contains("</" + tag + ">")
                && (n <= 0 || i < n))
            {
                int tagLength = tag.Length;
                int begin = text.IndexOf("<" + tag + ">") + tagLength + 2;
                int end = text.IndexOf("</" + tag + ">");
                string s = text.Substring(begin, end - begin);
                s = s.Replace("amp;", "");
                text = text.Substring(end + tagLength + 4);
                returnList.Add(s);
                i++;
            }
            if (n == 1 && returnList.Count == 0)
                returnList.Add("");
            return returnList;
        }

        /** Converts a string of numbers to a long. */
        private long getLongFromString(string input)
        {
            long a = 0;
            long j = 1;
            for (int i = input.Length - 1; i >= 0; i--)
            {
                try
                {
                    char c = input.ToCharArray(i, 1)[0];
                    a += (c - '0') * j;
                    j *= 10;
                }
                catch (Exception) { }
            }
            return a;
        }

        List<int> createSortOrder(List<long> item)
        {
            List<int> order = new List<int>();
            for (int i = 0; i < item.Count; i++)
            {
                long current = item[i];
                int k = 0;
                for (int j = 0; j < item.Count; j++)
                {
                    if (item[j] < current)
                        k++;
                    if (item[j] == current && j < i)
                        k++;
                }
                order.Add(k);
            }
            return order;
        }

        T[] sortUsingGivenOrder<T>(List<T> item, List<int> order)
        {
            T[] sorted = new T[100];
            for (int i = 0; i < order.Count; i++)
            {
                int position = order[i];
                if (position < 100 && i < item.Count)
                    sorted[position] = item[i];
            }
            return sorted;
        }

        /** Sets text to correct case and translates if needed. */
        private string textCheck(string text)
        {
            if (text == null)
                text = "";
            if (CHINESE)
                text = translate(text);
            if ((bool)radioL2.IsChecked)
                text = text.ToLower();
            if ((bool)radioL3.IsChecked)
                text = text.ToUpper();
            return text;
        }

        /** Overload for arrival seconds */
        private string textCheck(long arrival)
        {
            if (arrival == 0)
                return "";
            string s;
            double timeDiff = arrival - (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds;
            double time = Math.Abs(timeDiff) / 60000D;
            if (timeDiff < 0)
                s = CHINESE ? "已離開" : "Gone";
            else if (Math.Round(time) <= 1)
                s = CHINESE ? "即將到達" : "Arriving ";
            else if (toggleSeconds.IsOn)
            {
                double min = Math.Floor(time);
                double sec = Math.Floor((time - min) * 60);
                s = min.ToString() + (sec < 10 ? ":0" : ":") + sec.ToString();
            }
            else
                s = Math.Round(time).ToString() + (CHINESE ? "分" : "min");
            return textCheck(s);
        }

        /** Overload for arrival clock time */
        private string textCheck(long arrival, string othertext)
        {
            if (arrival == 0)
                return "";
            string timeString = (new DateTime(1970, 1, 1) +
                TimeSpan.FromSeconds(arrival / 1000D)).ToLocalTime().ToString();
            timeString = timeString.Substring(timeString.IndexOf(" ") + 1);
            if (CHINESE)
            {
                bool am = timeString.Substring(timeString.IndexOf(" ")) == " AM";
                timeString = (am ? "上午 " : "下午 ") + timeString.Remove(timeString.IndexOf(" "));
            }
            return textCheck(" " + timeString + "     " + othertext);
        }

        private async void addStation(string station, bool refresh = true, bool forceSuccess = false)
        {
            enableControls(false);
            if (!station.Contains("_"))
                station = "1_" + station;
            string url = "http://api.pugetsound.onebusaway.org/api/where/stop/"
                + station +
                ".xml?key=09ce661b-1d78-4fc4-a77b-7c55455acadb&&includeReferences=false";
            string text = "";
            text = await websiteReader(url, forceSuccess);
            List<string> stop = XMLReader(text, "name", 1);
            if (stop[0] != "")
            {
                Paragraph paragraph = new Paragraph();
                Run run = new Run();
                run.Text = station + " (" + stop[0] + ")";
                paragraph.Inlines.Add(run);
                richTextBox.Blocks.Add(paragraph);
                STATIONS.Add(station);
                if (refresh)
                    TimerEvent10(null, null);
            }
            else
            {
                ContentDialog errorDialog = new ContentDialog
                {
                    Title = "Could not fetch stop!",
                    Content = "Please try again.",
                    CloseButtonText = "OK",
                    PrimaryButtonText = "Find more stops",
                    DefaultButton = ContentDialogButton.Close
                };
                try
                {
                    ContentDialogResult result = await errorDialog.ShowAsync();
                    if (result == ContentDialogResult.Primary)
                    {
                        await Launcher.LaunchUriAsync(new Uri("http://pugetsound.onebusaway.org/where/standard/"));
                    }
                }
                catch { }
            }
            enableControls(true);
        }

        private void textBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter && textBox.Text != "")
                button_Click_Add(null, null);
        }

        private void languageChanged(object sender, RoutedEventArgs e)
        {
            if ((bool)checkChinese.IsChecked && (bool)checkEnglish.IsChecked)
            {
                timerToggle.Start();
                CHINESE = !CHINESE;
                checkChinese.IsEnabled = true;
                checkEnglish.IsEnabled = true;
            }
            else
            {
                timerToggle.Stop();
                CHINESE = (bool)checkChinese.IsChecked;
                if (CHINESE)
                    checkChinese.IsEnabled = false;
                else
                    checkEnglish.IsEnabled = false;
            }
        }

        private void Flyout_Opened(object sender, object e)
        {
            textBox.Focus(FocusState.Keyboard);
        }

        private void slider_ValueChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            MAX_STATIONS = (int)slider.Value;
            for (int i = 0; i < 100; i++)
            {
                try
                {
                    if (i < MAX_STATIONS)
                    {
                        int scale = MAX_STATIONS < 5 ? 4 : MAX_STATIONS;
                        TEXT_ROUTE_NUMBER[i].FontSize = 384D / scale;
                        TEXT_DESTINATION[i].FontSize = 288D / scale;
                        TEXT_SMALL_TEXT[i].FontSize = 96D / scale;
                        TEXT_ARRIVAL[i].FontSize = 384D / scale;
                    }
                    else
                    {
                        TEXT_ROUTE_NUMBER[i].Text = "";
                        TEXT_DESTINATION[i].Text = "";
                        TEXT_SMALL_TEXT[i].Text = "";
                        TEXT_ARRIVAL[i].Text = "";
                    }
                }
                catch { }
            }
        }

        private void enableControls(bool enable)
        {
            buttonAdd.IsEnabled = enable;
            buttonAddRandom.IsEnabled = enable;
            buttonClear.IsEnabled = enable;
            buttonAddPreset.IsEnabled = enable;
            buttonPreset1.IsEnabled = enable;
            buttonPreset2.IsEnabled = enable;
            buttonPreset3.IsEnabled = enable;
            buttonPreset4.IsEnabled = enable;
            textBox.IsEnabled = enable;
            slider.IsEnabled = enable;
            progressBar.Visibility = enable ? Visibility.Collapsed : Visibility.Visible;
            textBox.Focus(FocusState.Keyboard);
        }

        private string translate(string s)
        {
            s = s.ToLower();
            // 24
            s = s.Replace("university of washington", "華盛頓大學");
            // 19
            s = s.Replace("passenger terminal", "客運大樓");
            // 16
            s = s.Replace("convention place", "會議中心");
            s = s.Replace("downtown seattle", "西雅圖市中心");
            // 15
            s = s.Replace("east green lake", "綠湖(東)");
            s = s.Replace("link light rail", "輕鐵");
            // 14
            s = s.Replace("ferry terminal", "渡輪碼頭");
            s = s.Replace("medical center", "醫療中心");
            s = s.Replace("point defiance", "蔑視角");
            s = s.Replace("transit center", "公共交匯處");
            // 13
            s = s.Replace("downtown only", "只往西雅圖市中心");
            s = s.Replace("international", "國際");
            s = s.Replace("port townsend", "湯森港");
            // 12
            s = s.Replace("all stations", "所有車站");
            s = s.Replace("capitol hill", "國會山區");
            // 11
            s = s.Replace("high school", "中學");
            s = s.Replace("laurelhurst", "月桂樹林中小丘");
            s = s.Replace("mount baker", "麵包山");
            s = s.Replace("park & ride", "泊車轉乘");
            s = s.Replace("the landing", "登陸");
            s = s.Replace("wallingford", "沃靈福德");
            // 10
            s = s.Replace("circulator", "循環綫");
            s = s.Replace("crossroads", "十字路口");
            s = s.Replace("fauntleroy", "方特勒羅伊");
            s = s.Replace("northbound", "北行");
            s = s.Replace("queen anne", "女王安妮");
            s = s.Replace("snoqualmie", "斯諾誇爾米");
            s = s.Replace("southbound", "南行");
            s = s.Replace("university", "大學");
            // 9
            s = s.Replace("alderwood", "奧德伍德");
            s = s.Replace("anacortes", "阿納科特斯");
            s = s.Replace("arlington", "阿靈頓");
            s = s.Replace("boulevard", "林蔭大道");
            s = s.Replace("bremerton", "布雷默頓");
            s = s.Replace("brickyard", "磚場");
            s = s.Replace("community", "社區");
            s = s.Replace("connector", "連接");
            s = s.Replace("chinatown", "唐人街");
            s = s.Replace("dwntn tac", "塔科馬市中心");
            s = s.Replace("education", "教育");
            s = s.Replace("roosevelt", "羅斯福");
            s = s.Replace("sheridian", "謝裡登");
            s = s.Replace("shoreline", "岸綫市");
            s = s.Replace("tahlequah", "塔勒");
            // 8
            s = s.Replace("bellevue", "貝爾維尤");
            s = s.Replace("broadway", "百老匯");
            s = s.Replace("cascadia", "卡斯卡迪亞");
            s = s.Replace("commerce", "商業");
            s = s.Replace("connecto", "連接");
            s = s.Replace("district", "區");
            s = s.Replace("downtown", "市中心");
            s = s.Replace("enumclaw", "恩努克勞");
            s = s.Replace("exchange", "交流");
            s = s.Replace("factoria", "法克特里亞");
            s = s.Replace("fairview", "錦繡");
            s = s.Replace("fairwood", "大快活");
            s = s.Replace("hospital", "醫院");
            s = s.Replace("houghton", "霍頓");
            s = s.Replace("issaquah", "伊瑟闊");
            s = s.Replace("junction", "連接點");
            s = s.Replace("kingston", "金士頓");
            s = s.Replace("kirkland", "柯克蘭");
            s = s.Replace("lynnwood", "林伍德");
            s = s.Replace("magnolia", "玉蘭");
            s = s.Replace("magnuson", "馬格努森");
            s = s.Replace("mckinley", "麥金利");
            s = s.Replace("meridian", "子午綫");
            s = s.Replace("mt baker", "麵包山");
            s = s.Replace("mukilteo", "慕基特奧");
            s = s.Replace("outbound", "往外");
            s = s.Replace("prentice", "徒弟");
            s = s.Replace("puyallup", "皮阿拉普");
            s = s.Replace("stanwood", "斯坦伍德");
            // 7
            s = s.Replace("admiral", "大將");
            s = s.Replace("airport", "機場");
            s = s.Replace("ballard", "巴拉德");
            s = s.Replace("bothell", "波塞爾");
            s = s.Replace("carkeek", "卡基克");
            s = s.Replace("central", "中");
            s = s.Replace("college", "學院");
            s = s.Replace("commons", "工地");
            s = s.Replace("diamond", "鑽石");
            s = s.Replace("edmonds", "埃德蒙兹");
            s = s.Replace("everett", "埃弗雷特");
            s = s.Replace("express", "特快");
            s = s.Replace("federal", "聯邦");
            s = s.Replace("fremont", "弗里蒙特");
            s = s.Replace("granite", "花崗岩");
            s = s.Replace("gregory", "格雷戈里");
            s = s.Replace("heights", "高原");
            s = s.Replace("inbound", "往内");
            s = s.Replace("jackson", "積臣");
            s = s.Replace("juanita", "胡安妮塔");
            s = s.Replace("judkins", "賈金斯");
            s = s.Replace("kenmore", "肯莫爾");
            s = s.Replace("kinnear", "金尼爾");
            s = s.Replace("landing", "登陸");
            s = s.Replace("madigan", "馬迪根");
            s = s.Replace("madison", "麥迪遜");
            s = s.Replace("madrona", "石南");
            s = s.Replace("mariner", "水手");
            s = s.Replace("olympia", "奧林匹亞");
            s = s.Replace("othello", "奧賽羅");
            s = s.Replace("rainier", "多雨");
            s = s.Replace("ravenna", "拉文納");
            s = s.Replace("redmond", "雷德蒙德");
            s = s.Replace("pacific", "太平洋");
            s = s.Replace("pioneer", "先鋒");
            s = s.Replace("proctor", "監考");
            s = s.Replace("sea tac", "西塔科");
            s = s.Replace("sea-tac", "西塔科");
            s = s.Replace("seattle", "西雅圖");
            s = s.Replace("shuttle", "穿梭");
            s = s.Replace("sounder", "聲音");
            s = s.Replace("station", "站");
            s = s.Replace("stevens", "司提反");
            s = s.Replace("terrace", "陽台");
            s = s.Replace("theater", "劇院");
            s = s.Replace("tukwila", "塔克維拉");
            s = s.Replace("tulalip", "圖拉利普");
            s = s.Replace("up-town", "住宅區");
            s = s.Replace("village", "村");
            s = s.Replace("warrior", "戰士");
            // 6
            s = s.Replace("alaska", "阿拉斯加");
            s = s.Replace("auburn", "奧本");
            s = s.Replace("aurora", "極光");
            s = s.Replace("beacon", "烽火");
            s = s.Replace("benson", "本森");
            s = s.Replace("boeing", "波音");
            s = s.Replace("burien", "布里恩");
            s = s.Replace("canyon", "峽谷");
            s = s.Replace("casino", "賭場");
            s = s.Replace("center", "中心");
            s = s.Replace("cherry", "櫻桃");
            s = s.Replace("church", "教會");
            s = s.Replace("colman", "科爾曼");
            s = s.Replace("fedway", "聯邦道");
            s = s.Replace("friday", "星期五");
            s = s.Replace("george", "佐治");
            s = s.Replace("haller", "哈勒");
            s = s.Replace("harbor", "港");
            s = s.Replace("island", "島");
            s = s.Replace("mercer", "默瑟");
            s = s.Replace("milton", "米爾頓");
            s = s.Replace("mirror", "鏡");
            s = s.Replace("monroe", "夢露");
            s = s.Replace("pierce", "刺穿");
            s = s.Replace("purple", "紫");
            s = s.Replace("renton", "蘭頓");
            s = s.Replace("seatac", "西塔科");
            s = s.Replace("seward", "蘇厄德");
            s = s.Replace("silver", "銀");
            s = s.Replace("smokey", "煙熏");
            s = s.Replace("square", "廣場");
            s = s.Replace("street", "街");
            s = s.Replace("summit", "高峰");
            s = s.Replace("tacoma", "塔科馬");
            s = s.Replace("uptown", "住宅區");
            s = s.Replace("valley", "谷");
            s = s.Replace("vashon", "瓦雄");
            s = s.Replace("woodin", "伍丁");
            s = s.Replace("yakima", "亞基馬");
            s = s.Replace("yesler", "是的");
            // 5
            s = s.Replace("angle", "角度");
            s = s.Replace("arbor", "喬木");
            s = s.Replace("beach", "沙灘");
            s = s.Replace("black", "黑");
            s = s.Replace("bridge", "橋");
            s = s.Replace("clyde", "克萊德");
            s = s.Replace("coupe", "庫珀");
            s = s.Replace("creek", "溪");
            s = s.Replace("crest", "波峰");
            s = s.Replace("falls", "瀑布");
            s = s.Replace("first", "第一");
            s = s.Replace("green", "綠");
            s = s.Replace("inter", "間");
            s = s.Replace("kings", "國王");
            s = s.Replace("lakes", "湖");
            s = s.Replace("lands", "地");
            s = s.Replace("lewis", "路易斯");
            s = s.Replace("lopez", "洛佩兹");
            s = s.Replace("lower", "下");
            s = s.Replace("loyal", "忠誠");
            s = s.Replace("maple", "楓");
            s = s.Replace("marys", "馬里斯");
            s = s.Replace("mount", "山");
            s = s.Replace("north", "北");
            s = s.Replace("orcas", "鯨魚");
            s = s.Replace("pearl", "珍珠");
            s = s.Replace("point", "角");
            s = s.Replace("purdy", "珀迪");
            s = s.Replace("ridge", "嶺");
            s = s.Replace("river", "河");
            s = s.Replace("shill", "南山"); // idk if this is right?
            s = s.Replace("shore", "岸");
            s = s.Replace("south", "南");
            s = s.Replace("super", "非常好");
            s = s.Replace("swamp", "沼澤");
            s = s.Replace("swift", "迅速");
            s = s.Replace("totem", "圖騰");
            s = s.Replace("union", "聯合");
            s = s.Replace("ville", "維爾");
            s = s.Replace("white", "白");
            s = s.Replace("worth", "價值");
            // 4
            s = s.Replace("bain", "貝恩");
            s = s.Replace("bend", "彎");
            s = s.Replace("blue", "藍");
            s = s.Replace("city", "城");
            s = s.Replace("coll", "學院");
            s = s.Replace("dash", "衝");
            s = s.Replace("dome", "拱頂");
            s = s.Replace("down", "下");
            s = s.Replace("dtwn", "市中心");
            s = s.Replace("dwtn", "市中心");
            s = s.Replace("east", "東");
            s = s.Replace("ever", "常");
            s = s.Replace("fair", "公平");
            s = s.Replace("firs", "冷杉");
            s = s.Replace("gate", "閘");
            s = s.Replace("gold", "金");
            s = s.Replace("high", "高");
            s = s.Replace("hill", "山");
            s = s.Replace("hoyt", "霍伊特");
            s = s.Replace("kent", "肯德");
            s = s.Replace("king", "國王");
            s = s.Replace("lake", "湖");
            s = s.Replace("land", "地");
            s = s.Replace("leaf", "葉");
            s = s.Replace("line", "綫");
            s = s.Replace("link", "輕鐵");
            s = s.Replace("main", "正");
            s = s.Replace("mall", "商場");
            s = s.Replace("mont", "蒙");
            s = s.Replace("over", "上");
            s = s.Replace("park", "公園");
            s = s.Replace("pike", "梭魚");
            s = s.Replace("port", "港");
            s = s.Replace("post", "郵政");
            s = s.Replace("shaw", "肖");
            s = s.Replace("tech", "科技");
            s = s.Replace("town", "市");
            s = s.Replace("road", "路");
            s = s.Replace("sand", "沙");
            s = s.Replace("stcr", "電車");
            s = s.Replace("twin", "雙");
            s = s.Replace("vasa", "瓦薩");
            s = s.Replace("wedg", "楔");
            s = s.Replace("west", "西");
            s = s.Replace("wood", "木");
            s = s.Replace("zone", "區");
            // 3
            s = s.Replace("and", "及");
            s = s.Replace("ash", "灰");
            s = s.Replace("ave", "大街");
            s = s.Replace("bar", "條");
            s = s.Replace("fhs", "第一山");
            s = s.Replace("gig", "演出");
            s = s.Replace("p&r", "泊車轉乘");
            s = s.Replace("pac", "太平");
            s = s.Replace("sch", "西雅圖兒童醫院");
            s = s.Replace("slu", "南湖聯合");
            s = s.Replace("sta", "站");
            s = s.Replace("stn", "站");
            s = s.Replace("tac", "塔科馬");
            s = s.Replace("tcc", "塔科馬社區學院");
            s = s.Replace("via", "經");
            s = s.Replace("way", "道");
            // 2
            s = s.Replace("cc", "社區學院");
            s = s.Replace("ne", "東北");
            s = s.Replace("nw", "西北");
            s = s.Replace("rd", "路");
            s = s.Replace("se", "東南");
            s = s.Replace("sr", "國家路綫");
            s = s.Replace("st", "街");
            s = s.Replace("sw", "西南");
            s = s.Replace("tc", "公共交匯處");
            s = s.Replace("to", "前往");
            s = s.Replace("ts", "站");
            s = s.Replace("up", "上");
            s = s.Replace("uw", "華盛頓大學");
            s = s.Replace("va", "老兵行政");
            // 1
            if (!s.Contains("e 綫") && !s.Contains("e1"))
            {
                s = s.Replace(" n", " 北");
                s = s.Replace(" e", " 東");
                s = s.Replace(" s", " 南");
                s = s.Replace(" w", " 西");
                s = s.Replace("n ", "北 ");
                s = s.Replace("e ", "東 ");
                s = s.Replace("s ", "南 ");
                s = s.Replace("w ", "西 ");
            }

            return s.Trim().ToUpper();
        }
    }
}
