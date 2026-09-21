using System.Data;
using System.Net.Sockets;
using System.Windows.Forms.DataVisualization.Charting;
using Zhaoxi.Components;
using Zhaoxi.CustControls;
using Zhaoxi.IntelligentWaterStorageMonitorSystem.Models;
using Zhaoxi.IntelligentWaterStorageMonitorSystem.Utils;
using Zhaoxi.Utils;
using Timer = System.Windows.Forms.Timer;
using Modbus.Device;

namespace Zhaoxi.IntelligentWaterStorageMonitorSystem
{
    public partial class FrmMain : Form
    {
        public FrmMain()
        {
            InitializeComponent();
        }

        #region 窗口拖动
        Point p = new Point();//暂存首次按下的位置
        bool isMove = false;//标识是否拖动中
        private void panelTop_MouseDown(object sender, MouseEventArgs e)
        {
            p = e.Location;
            isMove = true;//启动拖动
        }

        private void panelTop_MouseMove(object sender, MouseEventArgs e)
        {
            if (isMove && e.Button == MouseButtons.Left)
            {
                this.Location += new Size(e.Location.X - p.X, e.Location.Y - p.Y);
            }
        }

        private void panelTop_MouseUp(object sender, MouseEventArgs e)
        {
            isMove = false;//结束拖动
        }
        #endregion

        string comPath = "Communication.txt";
        string excelPath = "DataAddressFile.xls";
        CommunicationSet comSet = null;//通信设置对象
        List<ParaInfo> paraList = new List<ParaInfo>();//参数列表
        Timer timer = new Timer();
        bool isConnected = false;//连接状态
        TcpClient tcpClient = null;//客户端连接对象
        IModbusMaster master = null;//通信主站
        Dictionary<string, decimal> dicDatas = new Dictionary<string, decimal>();//实时数据
        Dictionary<string, bool> dicStates = new Dictionary<string, bool>();//状态集合
        List<TrendData> trendDatas = new List<TrendData>();//曲线数据列表
        private void FrmMain_Load(object sender, EventArgs e)
        {
            //组态区的数据与状态初始化
            foreach (Control c in panelDatas.Controls)
            {
                if (c is UEllipseSwitch)
                {
                    UEllipseSwitch sw = (UEllipseSwitch)c;
                    sw.Checked = false;
                }
                else if (c is URotarySwitch)
                {
                    URotarySwitch rs = (URotarySwitch)c;
                    rs.SwitchState = false;
                }
                else if (c is ParaTextBox)
                {
                    ParaTextBox txt = (ParaTextBox)c;
                    txt.Value = 0;
                }
                else if (c is UGroupPanel2)
                {
                    foreach (Control cc in c.Controls)
                    {
                        if (cc is ParaTextBox)
                        {
                            ParaTextBox txt = (ParaTextBox)cc;
                            txt.Value = 0;
                        }
                    }
                }
            }

            //状态监测区的初始化
            foreach (Control c in gpStates.Controls)
            {
                if (c is UJogSwitch)
                {
                    UJogSwitch js = (UJogSwitch)c;
                    js.IsOn = false;
                }
                if (c is Label && c.Tag != null)
                {
                    Label lbl = (Label)c;
                    string tagStr = lbl.Tag.ToString();//参数名
                    if (tagStr.Contains("BreakState"))
                    {
                        lbl.Text = "无";
                    }
                    else if (tagStr.Contains("PumpState"))
                    {
                        lbl.Text = "已停止";
                    }
                    else if (tagStr.Contains("ValveState"))
                    {
                        lbl.Text = "已关闭";
                    }
                    lbl.ForeColor = Color.Gray;
                }
            }

            //用水情况区
            wbWaters.Value = 0;
            lblPlanWaters.Value = 0;
            lblActualWaters.Value = 0;

            //趋势曲线区
            chart1.Series.Clear();
            //添加默认曲线序列
            AddDefaultSeries();

            //环境数据监测区
            foreach (Control c in gpMeters.Controls)
            {
                if (c is ParaTextBox)
                {
                    ParaTextBox txt = (ParaTextBox)c;
                    txt.Value = 0;
                }
                else if (c is UMeter)
                {
                    ((UMeter)c).Value = 0;
                }
                else if (c is UCircleMeter)
                {
                    ((UCircleMeter)c).Value = 0;
                }
            }

            //通信控制区初始化
            //保存按钮的副本
            Utility.dicBtnSets.Add(btnConnect.Name, CopyBtnSets(btnConnect));
            Utility.dicBtnSets.Add(btnDisconnect.Name, CopyBtnSets(btnDisconnect));
            lblConnection.Text = "等待连接";
            lblConnection.ForeColor = Color.Red;
            btnConnect.EnableBtn();
            btnDisconnect.UnableBtn();

            //加载通信设置
            LoadCommunication();
            //加载数据配置信息
            LoadDataAddressConfig();

            //初始化定时器
            InitTimer();

        }



        //添加默认曲线序列
        private void AddDefaultSeries()
        {
            AddSplineSeries("主管压力", "MainOutPressure", Color.DodgerBlue);
            AddSplineSeries("主管流量", "MainOutFlow", Color.Orange);
        }

        private void AddSplineSeries(string seriesName, string paraName, Color color)
        {
            Series series = new Series(seriesName);
            series.ChartType = SeriesChartType.Spline;//图表类型
            series.Tag = paraName;
            series.BorderWidth = 2;
            series.Color = color;
            series.XValueType = ChartValueType.DateTime;//x轴的值类型
            chart1.Series.Add(series);
        }

        //创建一个指定按钮的副本----保存基本设置
        private UButton CopyBtnSets(UButton btn)
        {
            UButton btnCopy = new UButton()
            {
                BgColor = btn.BgColor,
                BgColor2 = btn.BgColor2,
                ForeColor = btn.ForeColor,
                FocusBgColor = btn.FocusBgColor,
                FocusForeColor = btn.FocusForeColor
            };
            return btnCopy;
        }

        private void LoadCommunication()
        {
            if (File.Exists(comPath))
            {
                string[] lines = File.ReadAllLines(comPath);
                if (lines.Length == 2)
                {
                    string ip = lines[0].Split(':')[1];//ip地址
                    int port = lines[1].Split(":")[1].GetInt();//端口
                    txtIP.Text = ip;
                    txtPort.Text = port.ToString();
                    comSet = new CommunicationSet()
                    {
                        IP = ip,
                        Port = port
                    };
                }
            }
            else
            {
                txtIP.Text = "127.0.0.1";
                txtPort.Text = "302";
            }
        }

        private void LoadDataAddressConfig()
        {
            DataTable dt = ExcelHelper.ExcelToDataTable(excelPath, "Sheet1", true);
            if (dt != null && dt.Rows.Count > 0)
            {
                foreach (DataRow dr in dt.Rows)
                {
                    ParaInfo para = new ParaInfo();
                    para.ParaName = dr["参数名"].ToString();
                    para.Address = dr["地址"].ToString().GetUShort();
                    para.FunctionCode = dr["功能区"].ToString().GetByte();
                    para.DataType = dr["数据类型"].ToString();
                    para.DCount = dr["小数位数"].ToString().GetInt();
                    paraList.Add(para);
                }
            }
        }

        private void InitTimer()
        {
            timer.Interval = 100;
            timer.Tick += (obj, ev) =>
            {
                foreach (Control c in panelDatas.Controls)
                {
                    if (c is UPipe)
                    {
                        UPipe pipe = (UPipe)c;
                        pipe.Invalidate();
                    }
                }

                uPipe11.Active = uPipe8.Active = rsPump_1.SwitchState;
                uPipe3.Active = swValve_1.Checked;
                uPipe2.Active = uPipe1.Active = swValve_M.Checked;
                uPipe9.Active = uPipe12.Active = rsPump_2.SwitchState;
                uPipe6.Active = uPipe4.Active = swValve_2.Checked;
                uPipe13.Active = uPipe10.Active = rsPump_3.SwitchState;
                uPipe7.Active = uPipe5.Active = swValve_3.Checked;
                if (swValve_2.Checked == false)
                    uPipe6.Active = uPipe7.Active = uPipe5.Active = swValve_3.Checked;
                if (rsPump_1.SwitchState || rsPump_2.SwitchState || rsPump_3.SwitchState)
                    uPipe11.Active = true;
                if (rsPump_2.SwitchState || rsPump_3.SwitchState)
                    uPipe12.Active = true;
            };

        }

        /// <summary>
        /// 通信连接处理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnConnect_Click(object sender, EventArgs e)
        {
            if (isConnected == false)
            {
                //连接处理
                try
                {
                    string ip = txtIP.Text;
                    int port = txtPort.Text.GetInt();
                    tcpClient = new TcpClient(ip, port);
                    master = ModbusIpMaster.CreateIp(tcpClient);
                    if (tcpClient.Connected)
                    {
                        isConnected = true;//已连接
                        lblConnection.Text = "已连接，监测中...";
                        lblConnection.ForeColor = Color.DeepSkyBlue;
                        txtIP.Enabled = false;
                        txtPort.Enabled = false;
                        btnConnect.UnableBtn();
                        btnDisconnect.EnableBtn();
                        //开启实时采集线程
                        StartDataMonitor();
                        timer.Start();
                    }

                }
                catch (Exception ex)
                {
                    MessageHelper.Error("异常提示", $"异常：{ex.Message}");
                    return;
                }
            }
        }



        /// <summary>
        /// 通信断开处理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            if (isConnected)
            {
                try
                {
                    tcpClient.Close();
                    isConnected = false;//断开 
                    lblConnection.Text = "等待连接";
                    lblConnection.ForeColor = Color.Red;
                    txtIP.Enabled = true;
                    txtPort.Enabled = true;
                    btnConnect.EnableBtn();
                    btnDisconnect.UnableBtn();
                    timer.Stop();
                }
                catch (Exception ex)
                {
                    MessageHelper.Error("异常提示", $"异常：{ex.Message}");
                    return;
                }
            }
        }


        /// <summary>
        /// 启动监测
        /// </summary>
        private void StartDataMonitor()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    if (isConnected == false)
                        break;
                    await ReadAndLoadDaas();
                    await Task.Delay(1000);
                }
            });
        }

        /// <summary>
        /// 读取与呈现
        /// </summary>
        /// <returns></returns>
        private async Task ReadAndLoadDaas()
        {
            //读取数据
            //1.实时数据
            ushort[] udatas = await master.ReadHoldingRegistersAsync(1, 0, 38);
            //2.设备与阀门的状态
            bool[] states = await master.ReadCoilsAsync(1, 0, 7);
            //3.故障状态
            bool[] breakStates = await master.ReadInputsAsync(1, 0, 3);

            //数据解析与存储
            if (udatas.Length > 0)
            {
                var paras = paraList.Where(p => p.FunctionCode == 3).ToList();
                float mainPressure = 0.00f;
                float mainFlow = 0.0f;
                foreach (var para in paras)
                {
                    ushort addr = para.Address;//地址
                    decimal val = 0.0m;
                    switch (para.DataType)
                    {
                        case "float":
                            int dcount = para.DCount;//小数的位数
                            ushort[] datas = udatas.Skip(addr).Take(2).ToArray();
                            float fVal = Utility.GetUshortToFloat(datas);
                            if (para.ParaName == "MainOutPressure")
                                mainPressure = fVal;
                            else if (para.ParaName == "MainOutFlow")
                                mainFlow = fVal;
                            val = (decimal)fVal;
                            break;
                        case "uint":
                            ushort[] datas1 = udatas.Skip(addr).Take(2).ToArray();
                            val = Utility.GetUshortToUInt(datas1);
                            break;
                        case "ushort":
                            ushort udata = udatas[addr];
                            val = udata;
                            break;
                    }
                    if (dicDatas.ContainsKey(para.ParaName))
                        dicDatas[para.ParaName] = val;
                    else
                        dicDatas.Add(para.ParaName, val);
                }

                TrendData trendData = new TrendData()
                {
                    CurTime = DateTime.Now,
                    MainOutPressure = mainPressure,
                    MainOutFlow = mainFlow
                };
                //添加数据缓存
                if (trendDatas.Count > 100)
                    trendDatas.RemoveAt(0);
                trendDatas.Add(trendData);
            }

            if (states.Length > 0)
            {
                DealStates(states, 1);
            }
            if (breakStates.Length > 0)
            {
                DealStates(breakStates, 2);
            }

            //呈现处理
            this.Invoke(new Action(() =>
            {
                //开关状态、数据
                rsPump_1.SwitchState = dicStates[rsPump_1.Text];
                rsPump_2.SwitchState = dicStates[rsPump_2.Text];
                rsPump_3.SwitchState = dicStates[rsPump_3.Text];
                swValve_M.Checked = dicStates[swValve_M.Text];
                swValve_1.Checked = dicStates[swValve_1.Text];
                swValve_2.Checked = dicStates[swValve_2.Text];
                swValve_3.Checked = dicStates[swValve_3.Text];
                bool state1 = false;
                if (swValve_1.Checked && rsPump_1.SwitchState)
                {
                    lblPressure_1.Value = dicDatas[lblPressure_1.ParaName];
                    lblFlow_1.Value = dicDatas[lblFlow_1.ParaName];
                    state1 = true;
                }
                else
                {
                    lblPressure_1.Value = 0;
                    lblFlow_1.Value = 0;
                }

                bool state2 = false;
                if (swValve_2.Checked && rsPump_2.SwitchState)
                {
                    lblPressure_2.Value = dicDatas[lblPressure_2.ParaName];
                    lblFlow_2.Value = dicDatas[lblFlow_2.ParaName];
                    state2 = true;
                }
                else
                {
                    lblPressure_2.Value = 0;
                    lblFlow_2.Value = 0;
                }

                bool state3 = false;
                if (swValve_3.Checked && rsPump_3.SwitchState)
                {
                    lblPressure_3.Value = dicDatas[lblPressure_3.ParaName];
                    lblFlow_3.Value = dicDatas[lblFlow_3.ParaName];
                    state3 = true;
                }
                else
                {
                    lblPressure_3.Value = 0;
                    lblFlow_3.Value = 0;
                }
                if (swValve_M.Checked && (state1 || state2 || state3))
                {
                    lblMainPressure.Value = dicDatas[lblMainPressure.ParaName];
                    lblMainFlow.Value = dicDatas[lblMainFlow.ParaName];
                }
                else
                {
                    lblMainPressure.Value = 0;
                    lblMainFlow.Value = 0;
                }
                //当前水位
                lblCurPos.Value = dicDatas[lblCurPos.ParaName];
                //水泵数据
                LoadPumpData(gpPumpData1, rsPump_1);
                LoadPumpData(gpPumpData2, rsPump_2);
                LoadPumpData(gpPumpData3, rsPump_3);
                //状态监测区
                foreach (Control c in gpStates.Controls)
                {
                    if (c is UJogSwitch)
                    {
                        UJogSwitch js = (UJogSwitch)c;
                        js.IsOn = dicStates[js.Text];//取状态
                    }
                    if (c is Label && c.Tag != null)
                    {
                        Label lbl = (Label)c;
                        string paraName = lbl.Tag.ToString();
                        bool state = dicStates[paraName];//状态
                        if (paraName.Contains("PumpState"))
                        {
                            if (!state)
                            {
                                lbl.Text = "已停止";
                                lbl.ForeColor = Color.Red;
                            }
                            else
                            {
                                lbl.Text = "运行中";
                                lbl.ForeColor = Color.Orange;
                            }
                        }
                        else if (paraName.Contains("ValveState"))
                        {
                            if (!state)
                            {
                                lbl.Text = "已关闭";
                                lbl.ForeColor = Color.Gray;
                            }
                            else
                            {
                                lbl.Text = "已打开";
                                lbl.ForeColor = Color.Orange;
                            }
                        }
                        else if (paraName.Contains("BreakState"))
                        {
                            if (!state)
                            {
                                int id = paraName.Split('-')[1].GetInt();
                                if (dicStates["PumpState-" + id])
                                {
                                    lbl.Text = "正常";
                                    lbl.ForeColor = Color.DodgerBlue;
                                }
                                else
                                {
                                    lbl.Text = "无";
                                    lbl.ForeColor = Color.Gray;
                                }
                            }
                            else
                            {
                                lbl.Text = "故障";
                                lbl.ForeColor = Color.Red;
                            }
                        }
                    }
                }

                //用水情况区
                lblPlanWaters.Value = dicDatas[lblPlanWaters.ParaName];
                lblActualWaters.Value = dicDatas[lblActualWaters.ParaName];
                //计算用水比例
                int value = (int)((lblActualWaters.Value / lblPlanWaters.Value) * 100);
                wbWaters.Value = value;

                //环境数据监测区
                foreach (Control c in gpMeters.Controls)
                {
                    if (c is ParaTextBox)
                    {
                        ParaTextBox txt = (ParaTextBox)c;
                        string paraName = txt.ParaName;
                        txt.Value = dicDatas[paraName];//取值
                    }
                    else if (c is UMeter)
                    {
                        UMeter meter = (UMeter)c;
                        meter.Value = (double)dicDatas[meter.Text];
                    }
                    else if (c is UCircleMeter)
                    {
                        UCircleMeter meter = (UCircleMeter)c;
                        meter.Value = (double)dicDatas[meter.Text];
                    }
                }
                //实时曲线刷新
                List<TrendData> dataList = new List<TrendData>();//临时集合，放最新的20条数据
                int actCount = trendDatas.Count;//实时数目
                int start = 0, end = 0;
                start = actCount <= 20 ? 0 : actCount - 20;//开始索引
                end = actCount;
                //取出最新的数据，可能小于20，等于20
                for (int i = start; i < end; i++)
                {
                    TrendData data = trendDatas[i];
                    dataList.Add(data);
                }
                foreach (var series in chart1.Series)
                {
                    string paraName = series.Tag.ToString();
                    series.Points.DataBind(dataList, "CurTime", paraName, "");
                }
            }));
        }

        /// <summary>
        /// 解析状态与存储
        /// </summary>
        /// <param name="states"></param>
        /// <param name="functionCode"></param>
        private void DealStates(bool[] states, byte functionCode)
        {
            var paras = paraList.Where(p => p.FunctionCode == functionCode).ToList();
            foreach (var para in paras)
            {
                ushort addr = para.Address;
                bool state = states[addr];
                if (dicStates.ContainsKey(para.ParaName))
                {
                    dicStates[para.ParaName] = state;
                }
                else
                    dicStates.Add(para.ParaName, state);
            }
        }

        //呈现指定水泵的数据
        private void LoadPumpData(UGroupPanel2 gp, URotarySwitch js)
        {
            foreach (Control c in gp.Controls)
            {
                if (c is ParaTextBox)
                {
                    ParaTextBox txt = (ParaTextBox)c;
                    string paraName = txt.ParaName;
                    if (js.SwitchState)
                    {
                        txt.Value = dicDatas[paraName];
                    }
                    else
                        txt.Value = 0;
                }
            }
        }

        /// <summary>
        /// 水泵的启停控制
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void rsPump_StateChanged(object sender, EventArgs e)
        {
            if (isConnected)
            {
                //获取控件
                URotarySwitch js = (URotarySwitch)sender;
                string paraName = js.Text;//状态参数名
                bool state = js.SwitchState;//要设置的状态
                ParaInfo para = paraList.Find(p => p.ParaName == paraName);
                ushort addr = para.Address;
                master.WriteSingleCoil(1, addr, state);//状态写入
                dicStates[paraName] = state;
            }
            else
            {
                MessageHelper.Error("错误提示", "当前未连接，不能进行水泵控制！");
                return;
            }
        }

        /// <summary>
        /// 阀门开关控制
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void swValve_CheckedChanged(object sender, EventArgs e)
        {
            if (isConnected)
            {
                //获取控件
                UEllipseSwitch sw = (UEllipseSwitch)sender;
                string paraName = sw.Text;//状态参数名
                bool state = sw.Checked;//要设置的状态
                ParaInfo para = paraList.Find(p => p.ParaName == paraName);
                ushort addr = para.Address;
                master.WriteSingleCoil(1, addr, state);//状态写入
                dicStates[paraName] = state;
            }
            else
            {
                MessageHelper.Error("错误提示", "当前未连接，不能进阀门控制！");
                return;
            }
        }

        /// <summary>
        /// 退出系统
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void FrmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if(MessageHelper.Question("退出提示","你确定要退出系统吗？")==DialogResult.OK)
            {
                //连接是否断开
                if(isConnected)
                {
                    timer.Stop();
                    tcpClient.Close();
                    master.Dispose();
                    isConnected = false;
                }
                Application.ExitThread();
            }
            else 
                e.Cancel = true;//取消退出
        }
    }
}
