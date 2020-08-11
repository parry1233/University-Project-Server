using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net;//
using System.Net.Sockets;//
using System.Threading;//
using System.ComponentModel;//
using System.Data;//
using MySql.Data;//
using MySql.Data.MySqlClient;//
using System.Net.Mail;//

namespace CCU2._0_Server
{
	/// <summary>
	/// MainWindow.xaml 的互動邏輯
	/// </summary>
	public partial class MainWindow : Window
	{
		//static string dbHost = "ccuigo.czzx5egn4lyl.ap-northeast-1.rds.amazonaws.com";
		static string dbHost = "ccuigo.mysql.database.azure.com";
		static string dbPort = "3306";
		//static string dbUser = "admin";
		//static string dbPass = "admin0000";
		static string dbUser = "parry1233@ccuigo";
		static string dbPass = "Parry1000033";
		//static string dbName = "igo";
		static string dbName = "ccuigo";
		static string conn_info = "server=" + dbHost + ";port=" + dbPort + ";user=" + dbUser + ";password=" + dbPass + ";database=" + dbName + ";charset=utf8;oldguids=true;";
		Socket socketListen;//用於監聽的socket
		Socket socketConnect;//用於通訊的socket
		string RemoteEndPoint;//客戶端的網路節點
		Dictionary<string, Socket> dicClient = new Dictionary<string, Socket>();//連線的客戶端集合
		//Dictionary<string, string> dicInChatroom = new Dictionary<string, string>();
		//Dictionary<string, int> dicExistChatroom = new Dictionary<string, int>();
		List<Dictionary<string, Dictionary<string, string>>> ChatRoomList = new List<Dictionary<string, Dictionary<string, string>>>();

		LoadWindow loadWindow;
		BackgroundWorker worker;
		bool checkDatabaseConnect = false;
		Dictionary<string, string> dicRDMText = new Dictionary<string, string>();
		//MySqlConnection conn;//MySQL連線
		public MainWindow()
		{
			InitializeComponent();
			worker = new BackgroundWorker();
			worker.WorkerReportsProgress = true;
			worker.DoWork += worker_DoWork;
			worker.ProgressChanged += worker_ProgressChanged;
			worker.RunWorkerCompleted += worker_ProgressCompleted;
			OpenServer();
			LoadingBTN.Visibility = Visibility.Hidden;
			LoadingBTN.Visibility = Visibility.Collapsed;
			/*worker.RunWorkerAsync();
			loadWindow = new LoadWindow();
			loadWindow.ShowDialog();*/
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			
		}

		private void Window_ContentRendered(object sender, EventArgs e)
		{
			
		}

		void worker_DoWork(object sender, DoWorkEventArgs e)
		{
			for (int i = 0; i < 100; i++)
			{
				(sender as BackgroundWorker).ReportProgress(i);
				Thread.Sleep(25);
			}
		}

		void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			ProgressTXT.Text = e.ProgressPercentage.ToString()+"%";
			//loadWindow.ConnectProgress.Value = e.ProgressPercentage;
			//loadWindow.PercentLabel.Content = e.ProgressPercentage.ToString() + "%";
			if(e.ProgressPercentage<=20)
			{
				//loadWindow.Close();
				ConditionTXT.Content = "建立連線中";
			}
			else if (e.ProgressPercentage <= 60 && e.ProgressPercentage>20)
			{
				//loadWindow.Close();
				ConditionTXT.Content = "連線至Azure";
			}
			else if (e.ProgressPercentage <= 90 && e.ProgressPercentage > 60)
			{
				//loadWindow.Close();
				ConditionTXT.Content = "嘗試讀取資料庫";
			}
			else if(e.ProgressPercentage>90)
			{
				ConditionTXT.Content = "即將完成";
			}
		}

		void worker_ProgressCompleted(object sender,RunWorkerCompletedEventArgs e)
		{
			LoadingBTN.Visibility = Visibility.Hidden;
			LoadingBTN.Visibility = Visibility.Collapsed;
			ConnectBTN.IsEnabled = true;
			if(checkDatabaseConnect)
			{
				LogTextBox.Text += "[ SERVER ] 連線至 Azure 線上資料庫成功.\r\n";
			}
			else
			{
				LogTextBox.Text += "[ SERVER ] (錯誤報告)連線至線上資料庫失敗.\r\n";
			}
		}

		private void OpenServer()
		{
			try
			{
				string ip = "192.168.1.106";
				string port = "13000";
				//建立套接字
				TcpClient tcpClient = new TcpClient();
				//IPAddress ipAddress = Dns.GetHostAddresses("ec2-18-177-223-191.ap-northeast-1.compute.amazonaws.com")[0];
				//IPEndPoint ipe = new IPEndPoint(ipAddress, int.Parse("6666"));
				IPEndPoint ipe = new IPEndPoint(IPAddress.Parse(ip), int.Parse(port));
				socketListen = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
				//繫結埠和IP
				socketListen.Bind(ipe);
				//設定監聽
				socketListen.Listen(10);//max length of the pending client connections queue=10(懸掛的最大客戶端連接數=10)
				//連線客戶端
				AsyncConnect(socketListen);
				//MessageBox.Show("OpenSuccess");

				//ServerIPLabel.Content += ipAddress.ToString();
				ServerIPLabel.Content += ip;
				ServerPortLabel.Content += port;
			}
			catch(Exception e)
			{
				MessageBox.Show("OpenServer: "+e.Message,"Server ERR");
			}
		}

		/// <summary>
		/// 連線到客戶端
		/// </summary>
		/// <param name="socket"></param>
		private void AsyncConnect(Socket socket)
		{
			try
			{
				socket.BeginAccept(asyncResult =>
				{
					//獲取客戶端套接字
					socketConnect = socket.EndAccept(asyncResult);
					RemoteEndPoint = socketConnect.RemoteEndPoint.ToString();
					dicClient.Add(RemoteEndPoint, socketConnect);//新增至客戶端集合
					//使用Dispatcher物件跨執行緒
					Action methodDelegate = delegate ()
					{
						ClientComboBox.Items.Add(RemoteEndPoint);//新增客戶端埠號
					};
					this.Dispatcher.BeginInvoke(methodDelegate);
					//AsyncSend(socketConnect, string.Format("歡迎你{0}", socketConnect.RemoteEndPoint));
					AsyncReceive(socketConnect);
					AsyncConnect(socketListen);
				}, null);


			}
			catch (Exception e)
			{
				MessageBox.Show("AsyncConnect: "+e.Message,"Server ERR");
			}
		}

		/// <summary>
		/// 接收訊息
		/// </summary>
		/// <param name="client"></param>
		private void AsyncReceive(Socket socket)
		{
			byte[] data = new byte[102400];
			try
			{
				//開始接收訊息
				socket.BeginReceive(data, 0, data.Length, SocketFlags.None,
				asyncResult =>
				{
					try
					{
						int length = socket.EndReceive(asyncResult);
						//MessageBox.Show(Encoding.UTF8.GetString(data));
						string s="";
						string[] instuction= { "" };
						char[] delimiterChars = {':','/' };//分隔符類型-->當遇到這些符號時切割字串
						s = Encoding.UTF8.GetString(data);
						/*
						 * 將client的回傳的字串data之'\0'刪去
						 */
						int string_count = s.IndexOf('\0');
						if (string_count >= 0)
						{
							s = s.Substring(0, string_count);
						}
						instuction = s.Split(delimiterChars);
						//使用Dispatcher物件跨執行緒
						Action methodDelegate = delegate ()
						{
							LogTextBox.Text += "[ "+socket.RemoteEndPoint.ToString() + " ] " + s +"\r\n";
						};
						this.Dispatcher.BeginInvoke(methodDelegate);
						//char[] delimiterChars = { ',', '.', ':', '\t','/' };//分隔符類型-->當遇到這些符號時切割字串
						//string[] instuction = s.Split(delimiterChars);
						if (instuction[0].Equals("LOGINTRY"))
						{
							try
							{
								string date = DateTime.Now.ToString("yyyy-MM-dd");
								string time = DateTime.Now.ToShortTimeString();
								time = time.Replace(":", "時") + "分";
								string dateAndTime = date + " " + time;

								bool check_loginSuccess = false;

								using (MySqlConnection conn = new MySqlConnection(conn_info))
								{
									conn.Open();
									//'binary' will let MySQL be case sensitive, distinguishing capital and lower case letters
									string checkAccountCMD = "SELECT * FROM user WHERE User_ID = binary @ID_in";
									MySqlCommand cmd = new MySqlCommand(checkAccountCMD, conn);
									cmd.Parameters.AddWithValue("@ID_in", instuction[1]);
									MySqlDataReader dataRead = cmd.ExecuteReader();

									if (dataRead.HasRows)
									{
										while (dataRead.Read())
										{
											if (dataRead["User_PW"].Equals(instuction[2]))
											{
												check_loginSuccess = true;
												AsyncSend(dicClient[socket.RemoteEndPoint.ToString()], "LOGIN_PERMIT:"
													+ dataRead["User_ID"] + "/" + dataRead["User_PW"] + "/" + dataRead["User_Name"] + "/" + dataRead["User_Department"] + "/"
													+ dataRead["User_Grade"] + "/" + dataRead["User_Gender"] + "/" + dataRead["User_Email"]+"/"+ dataRead["User_Level"]+ "/" 
													+ dataRead["User_Points"]+ "/" + dataRead["User_PersonalDetail"]+ "/" + dataRead["User_Friend"]+ "/" + dataRead["User_Achievement"]+ "/" 
													+ dateAndTime+ "/" + dataRead["User_ShortTarget"]+ "/" + dataRead["User_LongTarget"]);
												//send User acount info

												Action methodDelegate_LogIn = delegate ()
												{
													LogTextBox.Text += "[ SERVER ] 回應 " + socket.RemoteEndPoint.ToString()
													+ " (結果) Log In Success\r\n";
												};
												this.Dispatcher.BeginInvoke(methodDelegate_LogIn);
											}
											else
											{
												AsyncSend(dicClient[socket.RemoteEndPoint.ToString()], "LOGIN_FAILED");
												Action methodDelegate_LogIn = delegate ()
												{
													LogTextBox.Text += "[ SERVER ] 回應 " + socket.RemoteEndPoint.ToString()
													+ " (結果) Log In Failed\r\n";
												};
												this.Dispatcher.BeginInvoke(methodDelegate_LogIn);
											}
										}
									}
									else
									{
										AsyncSend(dicClient[socket.RemoteEndPoint.ToString()], "LOGIN_FAILED");
										Action methodDelegate_LogIn = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] 回應 " + socket.RemoteEndPoint.ToString()
													+ " (結果) Log In Failed\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_LogIn);
									}
								}
								if(check_loginSuccess)
								{
									using (MySqlConnection conn = new MySqlConnection(conn_info))
									{
										conn.Open();
										string changeLoginTime_CMD = "UPDATE user SET User_LastLogin = @time_update WHERE User_ID = binary @ID_in";
										MySqlCommand cmd = new MySqlCommand(changeLoginTime_CMD, conn);
										cmd.Parameters.AddWithValue("@ID_in", instuction[1]);
										cmd.Parameters.AddWithValue("@time_update", dateAndTime);
										cmd.ExecuteNonQuery();

										Action methodDelegate_LogIn = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] 回應 " + socket.RemoteEndPoint.ToString()
											+ " (結果) Update User[" + instuction[1] + "] last login time to " + dateAndTime + "\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_LogIn);
									}
								}
							}
							catch (MySqlException ex)
							{
								switch (ex.Number)
								{
									case 0:
										Action methodDelegate_ERR_0 = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] Unpredicted incident occured.Fail to connect to database.\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_ERR_0);
										//MessageBox.Show("Unpredicted incident occured. Fail to connect to database.");
										break;
									case 1042:
										Action methodDelegate_ERR_1042 = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] Database IP error. Please check again.\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_ERR_1042);
										//MessageBox.Show("IP error. Please check again.");
										break;
									case 1045:
										Action methodDelegate_ERR_1045 = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] Database User account or password error. Please check again.\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_ERR_1045);
										//MessageBox.Show("User account or password error. Please check again");
										break;
									case 1062:
										Action methodDelegate_ERR_1062 = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] User account already existed. Please check again.\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_ERR_1062);
										//MessageBox.Show("UUser account already existed. Please check again");
										break;
									case 1366:
										Action methodDelegate_ERR_1366 = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] Incorrect vslue while INSERT, cannot insert to MySQL.\r\n";
											AsyncSend(dicClient[socket.RemoteEndPoint.ToString()], "REGISTER_ERROR");
										};
										this.Dispatcher.BeginInvoke(methodDelegate_ERR_1366);
										//MessageBox.Show("UUser account already existed. Please check again");
										break;
								}
							}
						}
						else if (instuction[0].Equals("REGISTER_VERIFY_ID"))
						{
							try
							{
								using (MySqlConnection conn = new MySqlConnection(conn_info))
								{
									conn.Open();
									string checkAccountCMD = "SELECT * FROM user WHERE User_ID = @ID_in";
									MySqlCommand cmd = new MySqlCommand(checkAccountCMD, conn);
									cmd.Parameters.AddWithValue("@ID_in", instuction[1]);
									MySqlDataReader dataRead = cmd.ExecuteReader();

									if (dataRead.HasRows)
									{
										AsyncSend(dicClient[socket.RemoteEndPoint.ToString()], "ID_EXISTED");
										Action methodDelegate_VerifyId = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] 回應 " + socket.RemoteEndPoint.ToString()
													+ " (結果) ID_Existed\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_VerifyId);
									}
									else
									{
										AsyncSend(dicClient[socket.RemoteEndPoint.ToString()], "ID_OKAY");
										Action methodDelegate_VerifyId = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] 回應 " + socket.RemoteEndPoint.ToString()
													+ " (結果) ID_Available\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_VerifyId);
									}
								}
							}
							catch (MySqlException ex)
							{
								switch (ex.Number)
								{
									case 0:
										Action methodDelegate_ERR_0 = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] Unpredicted incident occured.Fail to connect to database.\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_ERR_0);
										//MessageBox.Show("Unpredicted incident occured. Fail to connect to database.");
										break;
									case 1042:
										Action methodDelegate_ERR_1042 = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] Database IP error. Please check again.\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_ERR_1042);
										//MessageBox.Show("IP error. Please check again.");
										break;
									case 1045:
										Action methodDelegate_ERR_1045 = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] Database User account or password error. Please check again.\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_ERR_1045);
										//MessageBox.Show("User account or password error. Please check again");
										break;
									case 1062:
										Action methodDelegate_ERR_1062 = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] User account already existed. Please check again.\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_ERR_1062);
										//MessageBox.Show("UUser account already existed. Please check again");
										break;
									case 1366:
										Action methodDelegate_ERR_1366 = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] Incorrect vslue while INSERT, cannot insert to MySQL.\r\n";
											AsyncSend(dicClient[socket.RemoteEndPoint.ToString()], "REGISTER_ERROR");
										};
										this.Dispatcher.BeginInvoke(methodDelegate_ERR_1366);
										//MessageBox.Show("UUser account already existed. Please check again");
										break;
								}
							}
						}
						else if (instuction[0].Equals("REGISTER_VERIFY_IDPW"))
						{
							try
							{
								using (MySqlConnection conn = new MySqlConnection(conn_info))
								{
									conn.Open();
									string checkAccountCMD = "SELECT * FROM user WHERE User_ID = @ID_in";
									MySqlCommand cmd = new MySqlCommand(checkAccountCMD, conn);
									cmd.Parameters.AddWithValue("@ID_in", instuction[1]);
									MySqlDataReader dataRead = cmd.ExecuteReader();

									if (dataRead.HasRows)
									{
										AsyncSend(dicClient[socket.RemoteEndPoint.ToString()], "NEXT_DENY");
										Action methodDelegate_VerifyId = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] 回應 " + socket.RemoteEndPoint.ToString()
													+ " (結果) DENY CLIENT NEXT STEP cause by ID_EXISTED\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_VerifyId);
									}
									else
									{
										AsyncSend(dicClient[socket.RemoteEndPoint.ToString()], "NEXT_PERMIT:" + instuction[1] + "/" + instuction[2]);
										Action methodDelegate_VerifyId = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] 回應 " + socket.RemoteEndPoint.ToString()
													+ " (結果) PERMIT CLIENT NEXT STEP\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_VerifyId);
									}
								}
							}
							catch (MySqlException ex)
							{
								switch (ex.Number)
								{
									case 0:
										Action methodDelegate_ERR_0 = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] Unpredicted incident occured.Fail to connect to database.\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_ERR_0);
										//MessageBox.Show("Unpredicted incident occured. Fail to connect to database.");
										break;
									case 1042:
										Action methodDelegate_ERR_1042 = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] Database IP error. Please check again.\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_ERR_1042);
										//MessageBox.Show("IP error. Please check again.");
										break;
									case 1045:
										Action methodDelegate_ERR_1045 = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] Database User account or password error. Please check again.\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_ERR_1045);
										//MessageBox.Show("User account or password error. Please check again");
										break;
									case 1062:
										Action methodDelegate_ERR_1062 = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] User account already existed. Please check again.\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_ERR_1062);
										//MessageBox.Show("UUser account already existed. Please check again");
										break;
									case 1366:
										Action methodDelegate_ERR_1366 = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] Incorrect vslue while INSERT, cannot insert to MySQL.\r\n";
											AsyncSend(dicClient[socket.RemoteEndPoint.ToString()], "REGISTER_ERROR");
										};
										this.Dispatcher.BeginInvoke(methodDelegate_ERR_1366);
										//MessageBox.Show("UUser account already existed. Please check again");
										break;
								}
							}
						}
						else if (instuction[0].Equals("REGISTER_VERIFY_EMAIL"))
						{
							/* Generate Random Code */
							Random rnd = new Random();
							var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
							var stringChars = new char[6];

							for (int i = 0; i < stringChars.Length; i++)
							{
								stringChars[i] = chars[rnd.Next(chars.Length)];
							}

							bool checkExist = false;
							foreach (KeyValuePair<string, string> item in dicRDMText)
							{
								if (socket.RemoteEndPoint.ToString().Equals(item.Key))
								{
									checkExist = true;
									dicRDMText[item.Key] = new string(stringChars);
									break;
								}
							}
							if (!checkExist)
							{
								dicRDMText.Add(socket.RemoteEndPoint.ToString(), new string(stringChars));
							}
							//this.RandomTXT = new string(stringChars);

							/* Send Email */
							try
							{
								System.Net.Mail.MailMessage msg = new System.Net.Mail.MailMessage();
								msg.To.Add(instuction[1]);
								//msg.To.Add("b@b.com");可以發送給多人
								//msg.CC.Add("c@c.com");
								//msg.CC.Add("c@c.com");可以抄送副本給多人 
								//這裡可以隨便填，不是很重要
								msg.From = new MailAddress(instuction[1], "CCUiGO", System.Text.Encoding.UTF8);
								/* 上面3個參數分別是發件人地址（可以隨便寫），發件人姓名，編碼*/
								msg.Subject = "Your Verify Code";//郵件標題
								msg.SubjectEncoding = System.Text.Encoding.UTF8;//郵件標題編碼
								msg.Body = "We appreciate you for using CCUiGO.\nYour Verify Code is: " + dicRDMText[socket.RemoteEndPoint.ToString()] + "."; //郵件內容
								msg.BodyEncoding = System.Text.Encoding.UTF8;//郵件內容編碼 
																			 //msg.Attachments.Add(new Attachment(@"D:\test2.docx"));  //附件
								msg.IsBodyHtml = true;//是否是HTML郵件 
													  //msg.Priority = MailPriority.High;//郵件優先級 

								SmtpClient client = new SmtpClient();
								client.Credentials = new System.Net.NetworkCredential("igoccu@gmail.com", "parry1233"); //這裡要填正確的帳號跟密碼
								client.Host = "smtp.gmail.com"; //設定smtp Server
								client.Port = 25; //設定Port
								client.EnableSsl = true; //gmail預設開啟驗證
								client.Send(msg); //寄出信件
								client.Dispose();
								msg.Dispose();

								Action methodDelegate_SendMail = delegate ()
								{
									LogTextBox.Text += "[ SERVER ] 回應 " + socket.RemoteEndPoint.ToString()
											+ " Send Email to: " + instuction[1] + " 驗證碼:" + dicRDMText[socket.RemoteEndPoint.ToString()] + "\r\n";
								};
								this.Dispatcher.BeginInvoke(methodDelegate_SendMail);
							}
							catch (Exception ex)
							{
								Action methodDelegate_SendMail = delegate ()
								{
									LogTextBox.Text += "Server ERR: " + ex.Message + "\r\n";
								};
								this.Dispatcher.BeginInvoke(methodDelegate_SendMail);
							}
						}
						else if (instuction[0].Equals("REGISTER_ACCOUNT"))
						{
							try
							{
								using (MySqlConnection conn = new MySqlConnection(conn_info))
								{
									conn.Open();
									if (instuction[8].Equals(dicRDMText[socket.RemoteEndPoint.ToString()]))
									{
										string registerAccountCMD = "INSERT INTO user (User_ID,User_PW,User_Name,User_Department,User_Grade,User_Gender,User_Email," +
										"User_Level,User_Points,User_PersonalDetail,User_Friend,User_Achievement,User_LastLogin,User_ShortTarget,User_LongTarget) " +
											"VALUES (@ID_in,@PW_in,@Name_in,@Depart_in,@Grade_in,@Gender_in,@Email_in,@ZERO_in,@ZERO_in,@NULL_in,@NULL_in,@NULL_in,@NULL_in,@NULL_in,@NULL_in)";
										MySqlCommand cmd = new MySqlCommand(registerAccountCMD, conn);
										cmd.Parameters.AddWithValue("@ID_in", instuction[1]);
										cmd.Parameters.AddWithValue("@PW_in", instuction[2]);
										cmd.Parameters.AddWithValue("@Name_in", instuction[3]);
										cmd.Parameters.AddWithValue("@Depart_in", instuction[4]);
										cmd.Parameters.AddWithValue("@Grade_in", instuction[5]);
										cmd.Parameters.AddWithValue("@Gender_in", instuction[6]);
										cmd.Parameters.AddWithValue("@Email_in", instuction[7]);
										cmd.Parameters.AddWithValue("@ZERO_in", "0");
										cmd.Parameters.AddWithValue("@NULL_in", "");
										cmd.ExecuteNonQuery();
										AsyncSend(dicClient[socket.RemoteEndPoint.ToString()], "REGISTER_ACCEPT");
										Action methodDelegate_VerifyId = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] 回應 " + socket.RemoteEndPoint.ToString()
													+ " (結果) 創建帳號完成(" + instuction[1] + "/" + instuction[2] + "/" + instuction[3] + "/" + instuction[4] + "/" +
													instuction[5] + "/" + instuction[6] + "/" + instuction[7] + ")" + "\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_VerifyId);
									}
									else
									{
										AsyncSend(dicClient[socket.RemoteEndPoint.ToString()], "REGISTER_DENY");
									}
								}
							}
							catch (MySqlException ex)
							{
								switch (ex.Number)
								{
									case 0:
										Action methodDelegate_ERR_0 = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] Unpredicted incident occured.Fail to connect to database.\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_ERR_0);
										//MessageBox.Show("Unpredicted incident occured. Fail to connect to database.");
										break;
									case 1042:
										Action methodDelegate_ERR_1042 = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] Database IP error. Please check again.\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_ERR_1042);
										//MessageBox.Show("IP error. Please check again.");
										break;
									case 1045:
										Action methodDelegate_ERR_1045 = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] Database User account or password error. Please check again.\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_ERR_1045);
										//MessageBox.Show("User account or password error. Please check again");
										break;
									case 1062:
										Action methodDelegate_ERR_1062 = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] User account already existed. Please check again.\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_ERR_1062);
										//MessageBox.Show("UUser account already existed. Please check again");
										break;
									case 1366:
										Action methodDelegate_ERR_1366 = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] Incorrect vslue while INSERT, cannot insert to MySQL.\r\n";
											AsyncSend(dicClient[socket.RemoteEndPoint.ToString()], "REGISTER_ERROR");
										};
										this.Dispatcher.BeginInvoke(methodDelegate_ERR_1366);
										//MessageBox.Show("UUser account already existed. Please check again");
										break;
								}
							}
						}
						else if (instuction[0].Equals("FOERGET_PW_CHECKIDEMAIL"))
						{
							try
							{
								using (MySqlConnection conn = new MySqlConnection(conn_info))
								{
									conn.Open();
									string checkAccountCMD = "SELECT * FROM user WHERE User_ID = binary @ID_in";
									MySqlCommand cmd = new MySqlCommand(checkAccountCMD, conn);
									cmd.Parameters.AddWithValue("@ID_in", instuction[1]);
									MySqlDataReader dataRead = cmd.ExecuteReader();

									if (dataRead.HasRows)
									{
										while (dataRead.Read())
										{
											if (dataRead["User_Email"].Equals(instuction[2]))
											{
												AsyncSend(dicClient[socket.RemoteEndPoint.ToString()], "FORGET_PW_IDEMAIL_PERMIT:" + dataRead["User_ID"] + "/" + dataRead["User_Email"]);

												Action methodDelegate_LogIn = delegate ()
												{
													LogTextBox.Text += "[ SERVER ] 回應 " + socket.RemoteEndPoint.ToString()
													+ " (結果) ForgetPW Request Permit, Now Send Verify Email\r\n";
												};
												this.Dispatcher.BeginInvoke(methodDelegate_LogIn);

												/* Generate Random Code */
												Random rnd = new Random();
												var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
												var stringChars = new char[6];

												for (int i = 0; i < stringChars.Length; i++)
												{
													stringChars[i] = chars[rnd.Next(chars.Length)];
												}

												bool checkExist = false;
												foreach (KeyValuePair<string, string> item in dicRDMText)
												{
													if (socket.RemoteEndPoint.ToString().Equals(item.Key))
													{
														checkExist = true;
														dicRDMText[item.Key] = new string(stringChars);
														break;
													}
												}
												if (!checkExist)
												{
													dicRDMText.Add(socket.RemoteEndPoint.ToString(), new string(stringChars));
												}
												//this.RandomTXT = new string(stringChars);

												/* Send Email */
												try
												{
													System.Net.Mail.MailMessage msg = new System.Net.Mail.MailMessage();
													msg.To.Add(instuction[2]);
													//msg.To.Add("b@b.com");可以發送給多人
													//msg.CC.Add("c@c.com");
													//msg.CC.Add("c@c.com");可以抄送副本給多人 
													//這裡可以隨便填，不是很重要
													msg.From = new MailAddress(instuction[2], "CCUiGO", System.Text.Encoding.UTF8);
													/* 上面3個參數分別是發件人地址（可以隨便寫），發件人姓名，編碼*/
													msg.Subject = "Your Verify Code";//郵件標題
													msg.SubjectEncoding = System.Text.Encoding.UTF8;//郵件標題編碼
													msg.Body = "We appreciate you for using CCUiGO.\nYour Verify Code is: " + dicRDMText[socket.RemoteEndPoint.ToString()] + "."; //郵件內容
													msg.BodyEncoding = System.Text.Encoding.UTF8;//郵件內容編碼 
																								 //msg.Attachments.Add(new Attachment(@"D:\test2.docx"));  //附件
													msg.IsBodyHtml = true;//是否是HTML郵件 
																		  //msg.Priority = MailPriority.High;//郵件優先級 

													SmtpClient client = new SmtpClient();
													client.Credentials = new System.Net.NetworkCredential("igoccu@gmail.com", "parry1233"); //這裡要填正確的帳號跟密碼
													client.Host = "smtp.gmail.com"; //設定smtp Server
													client.Port = 25; //設定Port
													client.EnableSsl = true; //gmail預設開啟驗證
													client.Send(msg); //寄出信件
													client.Dispose();
													msg.Dispose();

													Action methodDelegate_SendMail = delegate ()
													{
														LogTextBox.Text += "[ SERVER ] 回應 " + socket.RemoteEndPoint.ToString()
																+ " Send Email to: " + instuction[2] + " 驗證碼:" + dicRDMText[socket.RemoteEndPoint.ToString()] + "\r\n";
													};
													this.Dispatcher.BeginInvoke(methodDelegate_SendMail);
												}
												catch (Exception ex)
												{
													Action methodDelegate_SendMail = delegate ()
													{
														LogTextBox.Text += "Server ERR: " + ex.Message + "\r\n";
													};
													this.Dispatcher.BeginInvoke(methodDelegate_SendMail);
												}
											}
											else
											{
												AsyncSend(dicClient[socket.RemoteEndPoint.ToString()], "FORGET_PW_IDEMAIL_DENY");
												Action methodDelegate_LogIn = delegate ()
												{
													LogTextBox.Text += "[ SERVER ] 回應 " + socket.RemoteEndPoint.ToString()
													+ " (結果) ForgetPW Request Deny\r\n";
												};
												this.Dispatcher.BeginInvoke(methodDelegate_LogIn);
											}
										}
									}
									else
									{
										AsyncSend(dicClient[socket.RemoteEndPoint.ToString()], "FORGET_PW_IDEMAIL_DENY");
										Action methodDelegate_LogIn = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] 回應 " + socket.RemoteEndPoint.ToString()
											+ " (結果) ForgetPW Request Deny\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_LogIn);
									}
								}
							}
							catch (MySqlException ex)
							{
								switch (ex.Number)
								{
									case 0:
										Action methodDelegate_ERR_0 = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] Unpredicted incident occured.Fail to connect to database.\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_ERR_0);
										//MessageBox.Show("Unpredicted incident occured. Fail to connect to database.");
										break;
									case 1042:
										Action methodDelegate_ERR_1042 = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] Database IP error. Please check again.\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_ERR_1042);
										//MessageBox.Show("IP error. Please check again.");
										break;
									case 1045:
										Action methodDelegate_ERR_1045 = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] Database User account or password error. Please check again.\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_ERR_1045);
										//MessageBox.Show("User account or password error. Please check again");
										break;
									case 1062:
										Action methodDelegate_ERR_1062 = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] User account already existed. Please check again.\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_ERR_1062);
										//MessageBox.Show("User account already existed. Please check again");
										break;
									case 1366:
										Action methodDelegate_ERR_1366 = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] Incorrect vslue while INSERT, cannot insert to MySQL.\r\n";
											AsyncSend(dicClient[socket.RemoteEndPoint.ToString()], "REGISTER_ERROR");
										};
										this.Dispatcher.BeginInvoke(methodDelegate_ERR_1366);
										//MessageBox.Show("User account already existed. Please check again");
										break;
								}
							}
						}
						else if (instuction[0].Equals("FORGET_PW_VERIFY"))
						{
							if (instuction[1].Equals(dicRDMText[socket.RemoteEndPoint.ToString()]))
							{
								AsyncSend(dicClient[socket.RemoteEndPoint.ToString()], "FORGET_PW_VERIFY_PERMIT:" + instuction[2] + "/" + instuction[3]);
								Action methodDelegate_LogIn = delegate ()
								{
									LogTextBox.Text += "[ SERVER ] 回應 " + socket.RemoteEndPoint.ToString()
									+ " (結果) ForgetPW Verify Permit\r\n";
								};
								this.Dispatcher.BeginInvoke(methodDelegate_LogIn);
							}
							else
							{
								AsyncSend(dicClient[socket.RemoteEndPoint.ToString()], "FORGET_PW_VERIFY_DENY");
								Action methodDelegate_LogIn = delegate ()
								{
									LogTextBox.Text += "[ SERVER ] 回應 " + socket.RemoteEndPoint.ToString()
									+ " (結果) ForgetPW Verify Deny\r\n";
								};
								this.Dispatcher.BeginInvoke(methodDelegate_LogIn);
							}
						}
						else if (instuction[0].Equals("FORGET_PW_CHANGEPW"))
						{
							try
							{
								using (MySqlConnection conn = new MySqlConnection(conn_info))
								{
									conn.Open();
									string changePW_CMD = "UPDATE user SET User_PW = @PW_update WHERE User_ID = binary @ID";
									MySqlCommand cmd = new MySqlCommand(changePW_CMD, conn);
									cmd.Parameters.AddWithValue("@PW_update", instuction[1]);
									cmd.Parameters.AddWithValue("@ID", instuction[2]);
									cmd.ExecuteNonQuery();
									AsyncSend(dicClient[socket.RemoteEndPoint.ToString()], "CHANGE_PW_ACCEPT");
									Action methodDelegate_VerifyId = delegate ()
									{
										LogTextBox.Text += "[ SERVER ] 回應 " + socket.RemoteEndPoint.ToString()
												+ " (結果) 更改密碼完成(ID=" + instuction[2] + "/ New_PW=" + instuction[1] + "\r\n";
									};
									this.Dispatcher.BeginInvoke(methodDelegate_VerifyId);
								}
							}
							catch (MySqlException ex)
							{
								switch (ex.Number)
								{
									case 0:
										Action methodDelegate_ERR_0 = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] Unpredicted incident occured.Fail to connect to database.\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_ERR_0);
										//MessageBox.Show("Unpredicted incident occured. Fail to connect to database.");
										break;
									case 1042:
										Action methodDelegate_ERR_1042 = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] Database IP error. Please check again.\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_ERR_1042);
										//MessageBox.Show("IP error. Please check again.");
										break;
									case 1045:
										Action methodDelegate_ERR_1045 = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] Database User account or password error. Please check again.\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_ERR_1045);
										//MessageBox.Show("User account or password error. Please check again");
										break;
									case 1062:
										Action methodDelegate_ERR_1062 = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] User account already existed. Please check again.\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_ERR_1062);
										//MessageBox.Show("UUser account already existed. Please check again");
										break;
									case 1366:
										Action methodDelegate_ERR_1366 = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] Incorrect vslue while INSERT, cannot insert to MySQL.\r\n";
											AsyncSend(dicClient[socket.RemoteEndPoint.ToString()], "REGISTER_ERROR");
										};
										this.Dispatcher.BeginInvoke(methodDelegate_ERR_1366);
										//MessageBox.Show("UUser account already existed. Please check again");
										break;
								}
							}
						}
						else if (instuction[0].Equals("RESEND_VERIFYtoEMAIL"))
						{
							/* Generate Random Code */
							Random rnd = new Random();
							var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
							var stringChars = new char[6];

							for (int i = 0; i < stringChars.Length; i++)
							{
								stringChars[i] = chars[rnd.Next(chars.Length)];
							}

							bool checkExist = false;
							foreach (KeyValuePair<string, string> item in dicRDMText)
							{
								if (socket.RemoteEndPoint.ToString().Equals(item.Key))
								{
									checkExist = true;
									dicRDMText[item.Key] = new string(stringChars);
									break;
								}
							}
							if (!checkExist)
							{
								dicRDMText.Add(socket.RemoteEndPoint.ToString(), new string(stringChars));
							}
							//this.RandomTXT = new string(stringChars);

							/* Send Email */
							try
							{
								System.Net.Mail.MailMessage msg = new System.Net.Mail.MailMessage();
								msg.To.Add(instuction[1]);
								//msg.To.Add("b@b.com");可以發送給多人
								//msg.CC.Add("c@c.com");
								//msg.CC.Add("c@c.com");可以抄送副本給多人 
								//這裡可以隨便填，不是很重要
								msg.From = new MailAddress(instuction[1], "CCUiGO", System.Text.Encoding.UTF8);
								/* 上面3個參數分別是發件人地址（可以隨便寫），發件人姓名，編碼*/
								msg.Subject = "Your Verify Code";//郵件標題
								msg.SubjectEncoding = System.Text.Encoding.UTF8;//郵件標題編碼
								msg.Body = "We appreciate you for using CCUiGO.\nYour Verify Code is: " + dicRDMText[socket.RemoteEndPoint.ToString()] + "."; //郵件內容
								msg.BodyEncoding = System.Text.Encoding.UTF8;//郵件內容編碼 
																			 //msg.Attachments.Add(new Attachment(@"D:\test2.docx"));  //附件
								msg.IsBodyHtml = true;//是否是HTML郵件 
													  //msg.Priority = MailPriority.High;//郵件優先級 

								SmtpClient client = new SmtpClient();
								client.Credentials = new System.Net.NetworkCredential("igoccu@gmail.com", "parry1233"); //這裡要填正確的帳號跟密碼
								client.Host = "smtp.gmail.com"; //設定smtp Server
								client.Port = 25; //設定Port
								client.EnableSsl = true; //gmail預設開啟驗證
								client.Send(msg); //寄出信件
								client.Dispose();
								msg.Dispose();

								Action methodDelegate_SendMail = delegate ()
								{
									LogTextBox.Text += "[ SERVER ] 回應 " + socket.RemoteEndPoint.ToString()
											+ " Send Email to: " + instuction[1] + " 驗證碼:" + dicRDMText[socket.RemoteEndPoint.ToString()] + "\r\n";
								};
								this.Dispatcher.BeginInvoke(methodDelegate_SendMail);
							}
							catch (Exception ex)
							{
								Action methodDelegate_SendMail = delegate ()
								{
									LogTextBox.Text += "Server ERR: " + ex.Message + "\r\n";
								};
								this.Dispatcher.BeginInvoke(methodDelegate_SendMail);
							}
						}
						else if (instuction[0].Equals("GET_COMMENT_DEFAULT"))
						{
							try
							{
								using (MySqlConnection conn = new MySqlConnection(conn_info))
								{
									conn.Open();
									string searchClassDefaultCMD = "SELECT * FROM class";
									MySqlCommand cmd = new MySqlCommand(searchClassDefaultCMD, conn);
									MySqlDataReader dataRead = cmd.ExecuteReader();

									string sendBack = "/";
									if (dataRead.HasRows)
									{
										while (dataRead.Read())
										{
											string class_Name = dataRead["Class_Name"].ToString();
											class_Name = class_Name.Replace(":", "*");
											class_Name = class_Name.Replace("：", "*");
											class_Name = class_Name.Replace("/", "^");
											sendBack += class_Name + "/" + dataRead["Class_Department"] + "/" + dataRead["Class_Teacher"] + "/" + dataRead["Class_Ratings"] + "/";
										}
										sendBack = sendBack.Substring(0, sendBack.Length - 1);
										AsyncSend(dicClient[socket.RemoteEndPoint.ToString()], "GET_COMMENT_PERMIT:" + sendBack);
									}
									else
									{
										AsyncSend(dicClient[socket.RemoteEndPoint.ToString()], "Connect_Failed");
										Action methodDelegate_LogIn = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] 回應 " + socket.RemoteEndPoint.ToString()
													+ " (結果) Load Data Failed, Check MySQL Connection\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_LogIn);
									}
								}
							}
							catch (MySqlException ex)
							{
								switch (ex.Number)
								{
									case 0:
										Action methodDelegate_ERR_0 = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] Unpredicted incident occured.Fail to connect to database.\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_ERR_0);
										//MessageBox.Show("Unpredicted incident occured. Fail to connect to database.");
										break;
									case 1042:
										Action methodDelegate_ERR_1042 = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] Database IP error. Please check again.\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_ERR_1042);
										//MessageBox.Show("IP error. Please check again.");
										break;
									case 1045:
										Action methodDelegate_ERR_1045 = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] Database User account or password error. Please check again.\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_ERR_1045);
										//MessageBox.Show("User account or password error. Please check again");
										break;
									case 1062:
										Action methodDelegate_ERR_1062 = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] User account already existed. Please check again.\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_ERR_1062);
										//MessageBox.Show("UUser account already existed. Please check again");
										break;
									case 1366:
										Action methodDelegate_ERR_1366 = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] Incorrect vslue while INSERT, cannot insert to MySQL.\r\n";
											AsyncSend(dicClient[socket.RemoteEndPoint.ToString()], "REGISTER_ERROR");
										};
										this.Dispatcher.BeginInvoke(methodDelegate_ERR_1366);
										//MessageBox.Show("UUser account already existed. Please check again");
										break;
								}
							}
						}
						else if (instuction[0].Equals("GET_COMMENT_DEFAULT_FILTER_ALL"))
						{
							try
							{
								using (MySqlConnection conn = new MySqlConnection(conn_info))
								{
									conn.Open();

									string[] time_interval = instuction[1].Split(' ');

									string timeIntervalConcat = "";
									int counter1 = 0;
									foreach (string time in time_interval)
									{
										if (counter1 == 0)
										{
											timeIntervalConcat += time;
										}
										else
										{
											timeIntervalConcat += "|" + time;
										}
										counter1++;
									}

									string searchClassDefaultCMD = "SELECT * FROM class WHERE Class_Time REGEXP \"" + timeIntervalConcat + "\" AND Class_Ratings BETWEEN 0 AND " + instuction[2];
									MySqlCommand cmd = new MySqlCommand(searchClassDefaultCMD, conn);

									MySqlDataReader dataRead = cmd.ExecuteReader();

									string sendBack = "" + "/";
									int datacount = 0;
									if (dataRead.HasRows)
									{
										while (dataRead.Read())
										{
											datacount++;
											string class_Name = dataRead["Class_Name"].ToString();
											class_Name = class_Name.Replace(":", "*");
											class_Name = class_Name.Replace("：", "*");
											class_Name = class_Name.Replace("/", "^");
											sendBack += class_Name + "/" + dataRead["Class_Department"] + "/" + dataRead["Class_Teacher"] + "/" + dataRead["Class_Ratings"] + "/";
										}
										sendBack = sendBack.Substring(0, sendBack.Length - 1);
										AsyncSend(dicClient[socket.RemoteEndPoint.ToString()], "GET_COMMENT_PERMIT:" + sendBack);
										Action methodDelegate_LogIn = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] 回應 " + socket.RemoteEndPoint.ToString()
													+ " (結果) 回傳" + datacount + "筆課程資料\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_LogIn);
									}
									else
									{
										AsyncSend(dicClient[socket.RemoteEndPoint.ToString()], "SEARCH_NO_RESULT");
										Action methodDelegate_LogIn = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] 回應 " + socket.RemoteEndPoint.ToString()
													+ " (結果) 沒有結果，回傳 0 筆課程資料\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_LogIn);
									}
								}
							}
							catch (MySqlException ex)
							{
								switch (ex.Number)
								{
									case 0:
										Action methodDelegate_ERR_0 = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] Unpredicted incident occured.Fail to connect to database.\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_ERR_0);
										//MessageBox.Show("Unpredicted incident occured. Fail to connect to database.");
										break;
									case 1042:
										Action methodDelegate_ERR_1042 = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] Database IP error. Please check again.\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_ERR_1042);
										//MessageBox.Show("IP error. Please check again.");
										break;
									case 1045:
										Action methodDelegate_ERR_1045 = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] Database User account or password error. Please check again.\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_ERR_1045);
										//MessageBox.Show("User account or password error. Please check again");
										break;
									case 1062:
										Action methodDelegate_ERR_1062 = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] User account already existed. Please check again.\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_ERR_1062);
										//MessageBox.Show("UUser account already existed. Please check again");
										break;
									case 1366:
										Action methodDelegate_ERR_1366 = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] Incorrect vslue while INSERT, cannot insert to MySQL.\r\n";
											AsyncSend(dicClient[socket.RemoteEndPoint.ToString()], "REGISTER_ERROR");
										};
										this.Dispatcher.BeginInvoke(methodDelegate_ERR_1366);
										//MessageBox.Show("UUser account already existed. Please check again");
										break;
								}
							}
						}
						else if (instuction[0].Equals("GET_COMMENT_DEFAULT_FILTER_TIME"))
						{
							try
							{
								using (MySqlConnection conn = new MySqlConnection(conn_info))
								{
									conn.Open();

									string[] time_interval = instuction[1].Split(' ');

									string timeIntervalConcat = "";
									int counter1 = 0;
									foreach (string time in time_interval)
									{
										if (counter1 == 0)
										{
											timeIntervalConcat += time;
										}
										else
										{
											timeIntervalConcat += "|" + time;
										}
										counter1++;
									}

									string searchClassDefaultCMD = "SELECT * FROM class WHERE Class_Time REGEXP \"" + timeIntervalConcat + "\"";
									MySqlCommand cmd = new MySqlCommand(searchClassDefaultCMD, conn);

									MySqlDataReader dataRead = cmd.ExecuteReader();

									string sendBack = "" + "/";
									int datacount = 0;
									if (dataRead.HasRows)
									{
										while (dataRead.Read())
										{
											datacount++;
											string class_Name = dataRead["Class_Name"].ToString();
											class_Name = class_Name.Replace(":", "*");
											class_Name = class_Name.Replace("：", "*");
											class_Name = class_Name.Replace("/", "^");
											sendBack += class_Name + "/" + dataRead["Class_Department"] + "/" + dataRead["Class_Teacher"] + "/" + dataRead["Class_Ratings"] + "/";
										}
										sendBack = sendBack.Substring(0, sendBack.Length - 1);
										AsyncSend(dicClient[socket.RemoteEndPoint.ToString()], "GET_COMMENT_PERMIT:" + sendBack);
										Action methodDelegate_LogIn = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] 回應 " + socket.RemoteEndPoint.ToString()
													+ " (結果) 回傳" + datacount + "筆課程資料\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_LogIn);
									}
									else
									{
										AsyncSend(dicClient[socket.RemoteEndPoint.ToString()], "SEARCH_NO_RESULT");
										Action methodDelegate_LogIn = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] 回應 " + socket.RemoteEndPoint.ToString()
													+ " (結果) 沒有結果，回傳 0 筆課程資料\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_LogIn);
									}
								}
							}
							catch (MySqlException ex)
							{
								switch (ex.Number)
								{
									case 0:
										Action methodDelegate_ERR_0 = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] Unpredicted incident occured.Fail to connect to database.\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_ERR_0);
										//MessageBox.Show("Unpredicted incident occured. Fail to connect to database.");
										break;
									case 1042:
										Action methodDelegate_ERR_1042 = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] Database IP error. Please check again.\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_ERR_1042);
										//MessageBox.Show("IP error. Please check again.");
										break;
									case 1045:
										Action methodDelegate_ERR_1045 = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] Database User account or password error. Please check again.\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_ERR_1045);
										//MessageBox.Show("User account or password error. Please check again");
										break;
									case 1062:
										Action methodDelegate_ERR_1062 = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] User account already existed. Please check again.\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_ERR_1062);
										//MessageBox.Show("UUser account already existed. Please check again");
										break;
									case 1366:
										Action methodDelegate_ERR_1366 = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] Incorrect vslue while INSERT, cannot insert to MySQL.\r\n";
											AsyncSend(dicClient[socket.RemoteEndPoint.ToString()], "REGISTER_ERROR");
										};
										this.Dispatcher.BeginInvoke(methodDelegate_ERR_1366);
										//MessageBox.Show("UUser account already existed. Please check again");
										break;
								}
							}
						}
						else if (instuction[0].Equals("GET_COMMENT_DEFAULT_FILTER_RATE"))
						{
							try
							{
								using (MySqlConnection conn = new MySqlConnection(conn_info))
								{
									conn.Open();

									string searchClassDefaultCMD = "SELECT * FROM class WHERE Class_Ratings BETWEEN 0 AND " + instuction[1];
									MySqlCommand cmd = new MySqlCommand(searchClassDefaultCMD, conn);

									MySqlDataReader dataRead = cmd.ExecuteReader();

									string sendBack = "" + "/";
									int datacount = 0;
									if (dataRead.HasRows)
									{
										while (dataRead.Read())
										{
											datacount++;
											string class_Name = dataRead["Class_Name"].ToString();
											class_Name = class_Name.Replace(":", "*");
											class_Name = class_Name.Replace("：", "*");
											class_Name = class_Name.Replace("/", "^");
											sendBack += class_Name + "/" + dataRead["Class_Department"] + "/" + dataRead["Class_Teacher"] + "/" + dataRead["Class_Ratings"] + "/";
										}
										sendBack = sendBack.Substring(0, sendBack.Length - 1);
										AsyncSend(dicClient[socket.RemoteEndPoint.ToString()], "GET_COMMENT_PERMIT:" + sendBack);
										Action methodDelegate_LogIn = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] 回應 " + socket.RemoteEndPoint.ToString()
													+ " (結果) 回傳" + datacount + "筆課程資料\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_LogIn);
									}
									else
									{
										AsyncSend(dicClient[socket.RemoteEndPoint.ToString()], "SEARCH_NO_RESULT");
										Action methodDelegate_LogIn = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] 回應 " + socket.RemoteEndPoint.ToString()
													+ " (結果) 沒有結果，回傳 0 筆課程資料\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_LogIn);
									}
								}
							}
							catch (MySqlException ex)
							{
								switch (ex.Number)
								{
									case 0:
										Action methodDelegate_ERR_0 = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] Unpredicted incident occured.Fail to connect to database.\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_ERR_0);
										//MessageBox.Show("Unpredicted incident occured. Fail to connect to database.");
										break;
									case 1042:
										Action methodDelegate_ERR_1042 = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] Database IP error. Please check again.\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_ERR_1042);
										//MessageBox.Show("IP error. Please check again.");
										break;
									case 1045:
										Action methodDelegate_ERR_1045 = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] Database User account or password error. Please check again.\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_ERR_1045);
										//MessageBox.Show("User account or password error. Please check again");
										break;
									case 1062:
										Action methodDelegate_ERR_1062 = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] User account already existed. Please check again.\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_ERR_1062);
										//MessageBox.Show("UUser account already existed. Please check again");
										break;
									case 1366:
										Action methodDelegate_ERR_1366 = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] Incorrect vslue while INSERT, cannot insert to MySQL.\r\n";
											AsyncSend(dicClient[socket.RemoteEndPoint.ToString()], "REGISTER_ERROR");
										};
										this.Dispatcher.BeginInvoke(methodDelegate_ERR_1366);
										//MessageBox.Show("UUser account already existed. Please check again");
										break;
								}
							}
						}
						else if (instuction[0].Equals("GET_COMMENT_KEYWORD"))
						{
							try
							{
								using (MySqlConnection conn = new MySqlConnection(conn_info))
								{
									conn.Open();

									string[] keywords = instuction[1].Split(' ');
									string search_in_comment = "";
									string search_in_class = "";

									int counter = 0;
									foreach (string key in keywords)
									{
										if (counter ==0)
										{
											search_in_comment += "Comment_Tag LIKE \"%" + key + "%\" ";
											search_in_class += "CONCAT(Class_Department, Class_ID, Class_Name, Class_Teacher) LIKE \"%" + key + "%\" ";
										}
										else
										{
											search_in_comment += "AND Comment_Tag LIKE \"%" + key + "%\" ";
											search_in_class += "AND CONCAT(Class_Department, Class_ID, Class_Name, Class_Teacher) LIKE \"%" + key + "%\" ";
										}
										counter++;
									}
									//search_in_comment = search_in_comment.Substring(0, search_in_class.Length - 1);
									//search_in_class = search_in_class.Substring(0,search_in_class.Length-1);

									string searchClassDefaultCMD = "SELECT * FROM class WHERE Class_ID IN (SELECT Class_ID FROM comment WHERE " +search_in_comment+") " +
									"UNION SELECT * FROM class WHERE "+search_in_class;
									MySqlCommand cmd = new MySqlCommand(searchClassDefaultCMD, conn);
									foreach(string key in keywords)
									{
										cmd.Parameters.AddWithValue(key, key);
									}
									MySqlDataReader dataRead = cmd.ExecuteReader();

									string sendBack = instuction[1]+"/";
									int datacount=0;
									if (dataRead.HasRows)
									{
										while (dataRead.Read())
										{
											datacount++;
											string class_Name = dataRead["Class_Name"].ToString();
											class_Name = class_Name.Replace(":", "*");
											class_Name = class_Name.Replace("：", "*");
											class_Name = class_Name.Replace("/", "^");
											sendBack += class_Name + "/" + dataRead["Class_Department"] + "/" + dataRead["Class_Teacher"] + "/" + dataRead["Class_Ratings"] + "/";
										}
										sendBack = sendBack.Substring(0, sendBack.Length - 1);
										AsyncSend(dicClient[socket.RemoteEndPoint.ToString()], "GET_COMMENT_PERMIT:" + sendBack);
										Action methodDelegate_LogIn = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] 回應 " + socket.RemoteEndPoint.ToString()
													+ " (結果) 回傳"+datacount+"筆課程資料\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_LogIn);
									}
									else
									{
										AsyncSend(dicClient[socket.RemoteEndPoint.ToString()], "SEARCH_NO_RESULT");
										Action methodDelegate_LogIn = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] 回應 " + socket.RemoteEndPoint.ToString()
													+ " (結果) 沒有結果，回傳 0 筆課程資料\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_LogIn);
									}
								}
							}
							catch (MySqlException ex)
							{
								switch (ex.Number)
								{
									case 0:
										Action methodDelegate_ERR_0 = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] Unpredicted incident occured.Fail to connect to database.\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_ERR_0);
										//MessageBox.Show("Unpredicted incident occured. Fail to connect to database.");
										break;
									case 1042:
										Action methodDelegate_ERR_1042 = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] Database IP error. Please check again.\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_ERR_1042);
										//MessageBox.Show("IP error. Please check again.");
										break;
									case 1045:
										Action methodDelegate_ERR_1045 = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] Database User account or password error. Please check again.\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_ERR_1045);
										//MessageBox.Show("User account or password error. Please check again");
										break;
									case 1062:
										Action methodDelegate_ERR_1062 = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] User account already existed. Please check again.\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_ERR_1062);
										//MessageBox.Show("UUser account already existed. Please check again");
										break;
									case 1366:
										Action methodDelegate_ERR_1366 = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] Incorrect vslue while INSERT, cannot insert to MySQL.\r\n";
											AsyncSend(dicClient[socket.RemoteEndPoint.ToString()], "REGISTER_ERROR");
										};
										this.Dispatcher.BeginInvoke(methodDelegate_ERR_1366);
										//MessageBox.Show("UUser account already existed. Please check again");
										break;
								}
							}
						}
						else if (instuction[0].Equals("GET_COMMENT_KEYWORD_FILTER_ALL"))
						{
							try
							{
								using (MySqlConnection conn = new MySqlConnection(conn_info))
								{
									conn.Open();

									string[] keywords = instuction[1].Split(' ');
									string[] time_interval = instuction[2].Split(' ');
									string search_in_comment = "";
									string search_in_class = "";

									int counter = 0;
									foreach (string key in keywords)
									{
										if (counter == 0)
										{
											search_in_comment += "Comment_Tag LIKE \"%" + key + "%\" ";
											search_in_class += "CONCAT(Class_Department, Class_ID, Class_Name, Class_Teacher) LIKE \"%" + key + "%\" ";
										}
										else
										{
											search_in_comment += "AND Comment_Tag LIKE \"%" + key + "%\" ";
											search_in_class += "AND CONCAT(Class_Department, Class_ID, Class_Name, Class_Teacher) LIKE \"%" + key + "%\" ";
										}
										counter++;
									}

									string timeIntervalConcat = "";
									int counter1 = 0;
									foreach (string time in time_interval)
									{
										if(counter1==0)
										{
											timeIntervalConcat += time;
										}
										else
										{
											timeIntervalConcat += "|" + time;
										}
										counter1++;
									}

									string searchClassDefaultCMD = "SELECT * FROM class WHERE Class_ID IN (SELECT Class_ID FROM comment WHERE " + search_in_comment + ") " +
									"UNION SELECT * FROM class WHERE " + search_in_class+" AND Class_Time REGEXP \""+timeIntervalConcat+ "\" AND Class_Ratings BETWEEN 0 AND " +instuction[3];
									MySqlCommand cmd = new MySqlCommand(searchClassDefaultCMD, conn);
									foreach (string key in keywords)
									{
										cmd.Parameters.AddWithValue(key, key);
									}
									MySqlDataReader dataRead = cmd.ExecuteReader();

									string sendBack = instuction[1] + "/";
									int datacount = 0;
									if (dataRead.HasRows)
									{
										while (dataRead.Read())
										{
											datacount++;
											string class_Name = dataRead["Class_Name"].ToString();
											class_Name = class_Name.Replace(":", "*");
											class_Name = class_Name.Replace("：", "*");
											class_Name = class_Name.Replace("/", "^");
											sendBack += class_Name + "/" + dataRead["Class_Department"] + "/" + dataRead["Class_Teacher"] + "/" + dataRead["Class_Ratings"] + "/";
										}
										sendBack = sendBack.Substring(0, sendBack.Length - 1);
										AsyncSend(dicClient[socket.RemoteEndPoint.ToString()], "GET_COMMENT_PERMIT:" + sendBack);
										Action methodDelegate_LogIn = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] 回應 " + socket.RemoteEndPoint.ToString()
													+ " (結果) 回傳" + datacount + "筆課程資料\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_LogIn);
									}
									else
									{
										AsyncSend(dicClient[socket.RemoteEndPoint.ToString()], "SEARCH_NO_RESULT");
										Action methodDelegate_LogIn = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] 回應 " + socket.RemoteEndPoint.ToString()
													+ " (結果) 沒有結果，回傳 0 筆課程資料\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_LogIn);
									}
								}
							}
							catch (MySqlException ex)
							{
								switch (ex.Number)
								{
									case 0:
										Action methodDelegate_ERR_0 = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] Unpredicted incident occured.Fail to connect to database.\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_ERR_0);
										//MessageBox.Show("Unpredicted incident occured. Fail to connect to database.");
										break;
									case 1042:
										Action methodDelegate_ERR_1042 = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] Database IP error. Please check again.\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_ERR_1042);
										//MessageBox.Show("IP error. Please check again.");
										break;
									case 1045:
										Action methodDelegate_ERR_1045 = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] Database User account or password error. Please check again.\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_ERR_1045);
										//MessageBox.Show("User account or password error. Please check again");
										break;
									case 1062:
										Action methodDelegate_ERR_1062 = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] User account already existed. Please check again.\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_ERR_1062);
										//MessageBox.Show("UUser account already existed. Please check again");
										break;
									case 1366:
										Action methodDelegate_ERR_1366 = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] Incorrect vslue while INSERT, cannot insert to MySQL.\r\n";
											AsyncSend(dicClient[socket.RemoteEndPoint.ToString()], "REGISTER_ERROR");
										};
										this.Dispatcher.BeginInvoke(methodDelegate_ERR_1366);
										//MessageBox.Show("UUser account already existed. Please check again");
										break;
								}
							}
						}
						else if (instuction[0].Equals("GET_COMMENT_KEYWORD_FILTER_TIME"))
						{
							try
							{
								using (MySqlConnection conn = new MySqlConnection(conn_info))
								{
									conn.Open();

									string[] keywords = instuction[1].Split(' ');
									string[] time_interval = instuction[2].Split(' ');
									string search_in_comment = "";
									string search_in_class = "";

									int counter = 0;
									foreach (string key in keywords)
									{
										if (counter == 0)
										{
											search_in_comment += "Comment_Tag LIKE \"%" + key + "%\" ";
											search_in_class += "CONCAT(Class_Department, Class_ID, Class_Name, Class_Teacher) LIKE \"%" + key + "%\" ";
										}
										else
										{
											search_in_comment += "AND Comment_Tag LIKE \"%" + key + "%\" ";
											search_in_class += "AND CONCAT(Class_Department, Class_ID, Class_Name, Class_Teacher) LIKE \"%" + key + "%\" ";
										}
										counter++;
									}

									string timeIntervalConcat = "";
									int counter1 = 0;
									foreach (string time in time_interval)
									{
										if (counter1 == 0)
										{
											timeIntervalConcat += time;
										}
										else
										{
											timeIntervalConcat += "|" + time;
										}
										counter1++;
									}

									string searchClassDefaultCMD = "SELECT * FROM class WHERE Class_ID IN (SELECT Class_ID FROM comment WHERE " + search_in_comment + ") " +
									"UNION SELECT * FROM class WHERE " + search_in_class + " AND Class_Time REGEXP \"" + timeIntervalConcat + "\"";
									MySqlCommand cmd = new MySqlCommand(searchClassDefaultCMD, conn);
									foreach (string key in keywords)
									{
										cmd.Parameters.AddWithValue(key, key);
									}
									MySqlDataReader dataRead = cmd.ExecuteReader();

									string sendBack = instuction[1] + "/";
									int datacount = 0;
									if (dataRead.HasRows)
									{
										while (dataRead.Read())
										{
											datacount++;
											string class_Name = dataRead["Class_Name"].ToString();
											class_Name = class_Name.Replace(":", "*");
											class_Name = class_Name.Replace("：", "*");
											class_Name = class_Name.Replace("/", "^");
											sendBack += class_Name + "/" + dataRead["Class_Department"] + "/" + dataRead["Class_Teacher"] + "/" + dataRead["Class_Ratings"] + "/";
										}
										sendBack = sendBack.Substring(0, sendBack.Length - 1);
										AsyncSend(dicClient[socket.RemoteEndPoint.ToString()], "GET_COMMENT_PERMIT:" + sendBack);
										Action methodDelegate_LogIn = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] 回應 " + socket.RemoteEndPoint.ToString()
													+ " (結果) 回傳" + datacount + "筆課程資料\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_LogIn);
									}
									else
									{
										AsyncSend(dicClient[socket.RemoteEndPoint.ToString()], "SEARCH_NO_RESULT");
										Action methodDelegate_LogIn = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] 回應 " + socket.RemoteEndPoint.ToString()
													+ " (結果) 沒有結果，回傳 0 筆課程資料\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_LogIn);
									}
								}
							}
							catch (MySqlException ex)
							{
								switch (ex.Number)
								{
									case 0:
										Action methodDelegate_ERR_0 = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] Unpredicted incident occured.Fail to connect to database.\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_ERR_0);
										//MessageBox.Show("Unpredicted incident occured. Fail to connect to database.");
										break;
									case 1042:
										Action methodDelegate_ERR_1042 = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] Database IP error. Please check again.\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_ERR_1042);
										//MessageBox.Show("IP error. Please check again.");
										break;
									case 1045:
										Action methodDelegate_ERR_1045 = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] Database User account or password error. Please check again.\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_ERR_1045);
										//MessageBox.Show("User account or password error. Please check again");
										break;
									case 1062:
										Action methodDelegate_ERR_1062 = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] User account already existed. Please check again.\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_ERR_1062);
										//MessageBox.Show("UUser account already existed. Please check again");
										break;
									case 1366:
										Action methodDelegate_ERR_1366 = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] Incorrect vslue while INSERT, cannot insert to MySQL.\r\n";
											AsyncSend(dicClient[socket.RemoteEndPoint.ToString()], "REGISTER_ERROR");
										};
										this.Dispatcher.BeginInvoke(methodDelegate_ERR_1366);
										//MessageBox.Show("UUser account already existed. Please check again");
										break;
								}
							}
						}
						else if (instuction[0].Equals("GET_COMMENT_KEYWORD_FILTER_RATE"))
						{
							try
							{
								using (MySqlConnection conn = new MySqlConnection(conn_info))
								{
									conn.Open();

									string[] keywords = instuction[1].Split(' ');
									string search_in_comment = "";
									string search_in_class = "";

									int counter = 0;
									foreach (string key in keywords)
									{
										if (counter == 0)
										{
											search_in_comment += "Comment_Tag LIKE \"%" + key + "%\" ";
											search_in_class += "CONCAT(Class_Department, Class_ID, Class_Name, Class_Teacher) LIKE \"%" + key + "%\" ";
										}
										else
										{
											search_in_comment += "AND Comment_Tag LIKE \"%" + key + "%\" ";
											search_in_class += "AND CONCAT(Class_Department, Class_ID, Class_Name, Class_Teacher) LIKE \"%" + key + "%\" ";
										}
										counter++;
									}

									string searchClassDefaultCMD = "SELECT * FROM class WHERE Class_ID IN (SELECT Class_ID FROM comment WHERE " + search_in_comment + ") " +
									"UNION SELECT * FROM class WHERE " + search_in_class + " AND Class_Ratings BETWEEN 0 AND " + instuction[2];
									MySqlCommand cmd = new MySqlCommand(searchClassDefaultCMD, conn);
									foreach (string key in keywords)
									{
										cmd.Parameters.AddWithValue(key, key);
									}
									MySqlDataReader dataRead = cmd.ExecuteReader();

									string sendBack = instuction[1] + "/";
									int datacount = 0;
									if (dataRead.HasRows)
									{
										while (dataRead.Read())
										{
											datacount++;
											string class_Name = dataRead["Class_Name"].ToString();
											class_Name = class_Name.Replace(":", "*");
											class_Name = class_Name.Replace("：", "*");
											class_Name = class_Name.Replace("/", "^");
											sendBack += class_Name + "/" + dataRead["Class_Department"] + "/" + dataRead["Class_Teacher"] + "/" + dataRead["Class_Ratings"] + "/";
										}
										sendBack = sendBack.Substring(0, sendBack.Length - 1);
										AsyncSend(dicClient[socket.RemoteEndPoint.ToString()], "GET_COMMENT_PERMIT:" + sendBack);
										Action methodDelegate_LogIn = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] 回應 " + socket.RemoteEndPoint.ToString()
													+ " (結果) 回傳" + datacount + "筆課程資料\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_LogIn);
									}
									else
									{
										AsyncSend(dicClient[socket.RemoteEndPoint.ToString()], "SEARCH_NO_RESULT");
										Action methodDelegate_LogIn = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] 回應 " + socket.RemoteEndPoint.ToString()
													+ " (結果) 沒有結果，回傳 0 筆課程資料\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_LogIn);
									}
								}
							}
							catch (MySqlException ex)
							{
								switch (ex.Number)
								{
									case 0:
										Action methodDelegate_ERR_0 = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] Unpredicted incident occured.Fail to connect to database.\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_ERR_0);
										//MessageBox.Show("Unpredicted incident occured. Fail to connect to database.");
										break;
									case 1042:
										Action methodDelegate_ERR_1042 = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] Database IP error. Please check again.\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_ERR_1042);
										//MessageBox.Show("IP error. Please check again.");
										break;
									case 1045:
										Action methodDelegate_ERR_1045 = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] Database User account or password error. Please check again.\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_ERR_1045);
										//MessageBox.Show("User account or password error. Please check again");
										break;
									case 1062:
										Action methodDelegate_ERR_1062 = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] User account already existed. Please check again.\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_ERR_1062);
										//MessageBox.Show("UUser account already existed. Please check again");
										break;
									case 1366:
										Action methodDelegate_ERR_1366 = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] Incorrect vslue while INSERT, cannot insert to MySQL.\r\n";
											AsyncSend(dicClient[socket.RemoteEndPoint.ToString()], "REGISTER_ERROR");
										};
										this.Dispatcher.BeginInvoke(methodDelegate_ERR_1366);
										//MessageBox.Show("UUser account already existed. Please check again");
										break;
								}
							}
						}
						else if (instuction[0].Equals("GET_COMMENT"))
						{
							try
							{
								string classID = "";
								string classDepart = "";
								string classTeacher = "";
								string classRatings = "";
								string classTime = "";
								string className = instuction[1];
								string classNameString = instuction[1].Replace("*", ":");
								classNameString = classNameString.Replace("^", "/");
								using (MySqlConnection conn = new MySqlConnection(conn_info))
								{
									conn.Open();
									string searchCommentCMD = "SELECT * FROM class where Class_Name = @class_name AND Class_Department = @depart AND Class_Teacher = @teacher";
									MySqlCommand cmd = new MySqlCommand(searchCommentCMD, conn);
									cmd.Parameters.AddWithValue("@class_name",  classNameString );
									cmd.Parameters.AddWithValue("@depart", instuction[2]);
									cmd.Parameters.AddWithValue("@teacher", instuction[3]);
									MySqlDataReader dataRead = cmd.ExecuteReader();

									if (dataRead.HasRows)
									{
										while (dataRead.Read())
										{
											classID = dataRead["Class_ID"].ToString();
											classDepart = dataRead["Class_Department"].ToString();
											classTeacher = dataRead["Class_Teacher"].ToString();
											classRatings = dataRead["Class_Ratings"].ToString();
											classTime += "("+dataRead["Class_Time"].ToString()+")";
										}
									}
								}
								using (MySqlConnection conn = new MySqlConnection(conn_info))
								{
									conn.Open();
									string searchCommentCMD = "SELECT * FROM comment where Class_ID = @ID AND Class_Teacher = @teacher";
									MySqlCommand cmd = new MySqlCommand(searchCommentCMD, conn);
									cmd.Parameters.AddWithValue("@ID", classID);
									cmd.Parameters.AddWithValue("@teacher", classTeacher);
									MySqlDataReader dataRead = cmd.ExecuteReader();

									string sendBack = className + "/" + classID + "/" + classDepart + "/" + classTeacher + "/" +classRatings + "/"+classTime+"/";
									int datacount = 0;
									if (dataRead.HasRows)
									{
										while (dataRead.Read())
										{
											datacount++;
											string comment = dataRead["Comment_Content"].ToString();
											comment = comment.Replace(":", "*");
											comment = comment.Replace("：", "*");
											comment = comment.Replace("/", "^");
											sendBack += dataRead["User_ID"] + "/" + dataRead["Comment_Rating"] + "/" + comment + "/" +dataRead["Comment_Like"]+"/"+ dataRead["Comment_Date"] + "/" + dataRead["Comment_Tag"] + "/";
										}
										sendBack = sendBack.Substring(0, sendBack.Length - 1);
										AsyncSend(dicClient[socket.RemoteEndPoint.ToString()], "GET_COMMENT_DETAIL_PERMIT:" + sendBack);
										Action methodDelegate_LogIn = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] 回應 " + socket.RemoteEndPoint.ToString()
													+ " (結果) 回傳" + datacount + "筆評論資料\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_LogIn);
									}
									else
									{
										sendBack = sendBack.Substring(0, sendBack.Length - 1);
										AsyncSend(dicClient[socket.RemoteEndPoint.ToString()], "GET_COMMENT_DETAIL_PERMIT:"+sendBack);
										Action methodDelegate_LogIn = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] 回應 " + socket.RemoteEndPoint.ToString()
													+ " (結果) 沒有任何評論，回傳 0 筆評論資料\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_LogIn);
									}
								}
							}
							catch (MySqlException ex)
							{
								switch (ex.Number)
								{
									case 0:
										Action methodDelegate_ERR_0 = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] Unpredicted incident occured.Fail to connect to database.\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_ERR_0);
										//MessageBox.Show("Unpredicted incident occured. Fail to connect to database.");
										break;
									case 1042:
										Action methodDelegate_ERR_1042 = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] Database IP error. Please check again.\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_ERR_1042);
										//MessageBox.Show("IP error. Please check again.");
										break;
									case 1045:
										Action methodDelegate_ERR_1045 = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] Database User account or password error. Please check again.\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_ERR_1045);
										//MessageBox.Show("User account or password error. Please check again");
										break;
									case 1062:
										Action methodDelegate_ERR_1062 = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] User account already existed. Please check again.\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_ERR_1062);
										//MessageBox.Show("UUser account already existed. Please check again");
										break;
									case 1366:
										Action methodDelegate_ERR_1366 = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] Incorrect vslue while INSERT, cannot insert to MySQL.\r\n";
											AsyncSend(dicClient[socket.RemoteEndPoint.ToString()], "REGISTER_ERROR");
										};
										this.Dispatcher.BeginInvoke(methodDelegate_ERR_1366);
										break;
								}
							}
						}
						else if (instuction[0].Equals("COMMENT_REQUEST"))
						{
							string comment = instuction[5];
							comment = comment.Replace("*", ":");
							comment = comment.Replace("^", "/");
							int countExistComment = 0;
							try
							{
								using (MySqlConnection conn = new MySqlConnection(conn_info))
								{
									conn.Open();
									string CommentRowCountCMD = "SELECT COUNT(*) FROM comment";
									MySqlCommand cmd = new MySqlCommand(CommentRowCountCMD, conn);
									MySqlDataReader dataRead = cmd.ExecuteReader();

									if (dataRead.HasRows)
									{
										while (dataRead.Read())
										{
											countExistComment = Convert.ToInt32(dataRead["count(*)"].ToString());
										}
									}
								}
								using (MySqlConnection conn = new MySqlConnection(conn_info))
								{
									conn.Open();
									string addCommentCMD = "INSERT INTO comment (Comment_ID,User_ID,Class_ID,Class_Teacher,Comment_Rating,Comment_Content,Comment_Like,Comment_Date,Comment_Tag) " +
											"VALUES (@commentID,@userID,@class_ID,@teacher,@rate,@content,@like,@date,@tag)";
									MySqlCommand cmd = new MySqlCommand(addCommentCMD, conn);
									cmd.Parameters.AddWithValue("@commentID", countExistComment+1);
									cmd.Parameters.AddWithValue("@userID", instuction[1]);
									cmd.Parameters.AddWithValue("@class_ID", instuction[2]);
									cmd.Parameters.AddWithValue("@teacher", instuction[3]);
									cmd.Parameters.AddWithValue("@rate", instuction[4]);
									cmd.Parameters.AddWithValue("@content", comment);
									cmd.Parameters.AddWithValue("@like", instuction[6]);
									cmd.Parameters.AddWithValue("@date", instuction[7]);
									cmd.Parameters.AddWithValue("@tag", instuction[8]);
									cmd.ExecuteNonQuery();
									Action methodDelegate_VerifyId = delegate ()
									{
										LogTextBox.Text += "[ SERVER ] 回應 " + socket.RemoteEndPoint.ToString()
												+ " (結果) 新增評論完成\r\n";
									};
									this.Dispatcher.BeginInvoke(methodDelegate_VerifyId);
								}

								double rate=0.0;
								double comment_num=0;
								using (MySqlConnection conn = new MySqlConnection(conn_info))
								{
									conn.Open();
									string CommentRowCountCMD = "SELECT COUNT(*) FROM comment WHERE Class_ID = @cID";
									MySqlCommand cmd = new MySqlCommand(CommentRowCountCMD, conn);
									cmd.Parameters.AddWithValue("@cID", instuction[2]);
									MySqlDataReader dataRead = cmd.ExecuteReader();

									if (dataRead.HasRows)
									{
										while (dataRead.Read())
										{
											comment_num = Convert.ToDouble(dataRead["count(*)"].ToString());
										}
									}
								}
								using (MySqlConnection conn = new MySqlConnection(conn_info))
								{
									conn.Open();
									string CommentRowCountCMD = "SELECT Class_Ratings FROM class WHERE Class_ID = @ID AND Class_Teacher LIKE @teacher";
									MySqlCommand cmd = new MySqlCommand(CommentRowCountCMD, conn);
									cmd.Parameters.AddWithValue("@ID", instuction[2]);
									cmd.Parameters.AddWithValue("@teacher", "%" + instuction[3] + "%");
									MySqlDataReader dataRead = cmd.ExecuteReader();

									if (dataRead.HasRows)
									{
										while (dataRead.Read())
										{
											rate = Convert.ToDouble(dataRead["Class_Ratings"].ToString());
										}
									}
								}
								using (MySqlConnection conn = new MySqlConnection(conn_info))
								{
									conn.Open();
									string ReCalculateRate_CMD = "UPDATE class SET Class_Ratings = @Rate_update WHERE Class_ID = @ID AND Class_Teacher LIKE @teacher";
									MySqlCommand cmd = new MySqlCommand(ReCalculateRate_CMD, conn);
									double addRate = Convert.ToDouble(instuction[4].ToString());
									double newRate = ((rate * (comment_num-1.0)) + addRate) / (comment_num);
									cmd.Parameters.AddWithValue("@Rate_update", newRate.ToString("0.0"));
									cmd.Parameters.AddWithValue("@ID", instuction[2]);
									cmd.Parameters.AddWithValue("@teacher", "%" + instuction[3] + "%");
									cmd.ExecuteNonQuery();
									Action methodDelegate_VerifyId = delegate ()
									{
										LogTextBox.Text += "[ SERVER ] 回應 " + socket.RemoteEndPoint.ToString()
												+ " (結果) 更新課程評分完成\r\n";
									};
									this.Dispatcher.BeginInvoke(methodDelegate_VerifyId);
								}
								AsyncSend(dicClient[socket.RemoteEndPoint.ToString()], "ADD_COMMENT_ACCEPT");
							}
							catch(MySqlException ex)
							{
								switch (ex.Number)
								{
									case 0:
										Action methodDelegate_ERR_0 = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] Unpredicted incident occured.Fail to connect to database.\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_ERR_0);
										//MessageBox.Show("Unpredicted incident occured. Fail to connect to database.");
										break;
									case 1042:
										Action methodDelegate_ERR_1042 = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] Database IP error. Please check again.\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_ERR_1042);
										//MessageBox.Show("IP error. Please check again.");
										break;
									case 1045:
										Action methodDelegate_ERR_1045 = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] Database User account or password error. Please check again.\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_ERR_1045);
										//MessageBox.Show("User account or password error. Please check again");
										break;
									case 1062:
										Action methodDelegate_ERR_1062 = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] Comment ID already existed. Please check again.\r\n";
										};
										this.Dispatcher.BeginInvoke(methodDelegate_ERR_1062);
										//MessageBox.Show("UUser account already existed. Please check again");
										break;
									case 1366:
										Action methodDelegate_ERR_1366 = delegate ()
										{
											LogTextBox.Text += "[ SERVER ] Incorrect vslue while INSERT, cannot insert to MySQL.\r\n";
											AsyncSend(dicClient[socket.RemoteEndPoint.ToString()], "REGISTER_ERROR");
										};
										this.Dispatcher.BeginInvoke(methodDelegate_ERR_1366);
										break;
								}
							}
						}
						else if (instuction[0].Equals("PREVIEW_CHATROOM"))
						{
							string Room_info = "";
							try
							{
								foreach (Dictionary<string, Dictionary<string, string>> d in ChatRoomList)
								{
									foreach (KeyValuePair<string, Dictionary<string, string>> eachChatRoom in d)
									{
										Room_info += eachChatRoom.Key + "/" + eachChatRoom.Value.Count + "/";
									}
								}
								Room_info = Room_info.Substring(0, Room_info.Length - 1);
								AsyncSend(dicClient[socket.RemoteEndPoint.ToString()], "PREVIEW_CHATROOM_PERMIT:" + Room_info);
							}
							catch(System.ArgumentOutOfRangeException)
							{
								AsyncSend(dicClient[socket.RemoteEndPoint.ToString()], "PREVIEW_CHATROOM_PERMIT_NODATA");
							}
						}
						else if (instuction[0].Equals("ADD_CHATROOM"))
						{
							try
							{
								bool checkExist = false;
								foreach (Dictionary<string, Dictionary<string, string>> d in ChatRoomList)
								{
									foreach (KeyValuePair<string, Dictionary<string, string>> room in d)
									{
										if (room.Key.Equals(instuction[2]))
										{
											checkExist = true;
										}
									}
								}
								if(!checkExist)
								{
									Dictionary<string, string> member = new Dictionary<string, string>();
									member.Add(socket.RemoteEndPoint.ToString(), instuction[1]);
									Dictionary<string, Dictionary<string, string>> chatroom = new Dictionary<string, Dictionary<string, string>>();
									chatroom.Add(instuction[2], member);

									ChatRoomList.Add(chatroom);

									AsyncSend(dicClient[socket.RemoteEndPoint.ToString()], "ADD_CHATROOM_PERMIT:" + instuction[2]);
									foreach(KeyValuePair<string,Socket> onlineUser in dicClient)
									{
										if(!onlineUser.Key.Equals(socket.RemoteEndPoint.ToString()))
										{
											AsyncSend(dicClient[onlineUser.Key], "BROADCAST_ADD_TOPIC:" + instuction[1] + "/" + instuction[2]);
										}
									}
									Action methodDelegate_showResult = delegate ()
									{
										LogTextBox.Text += "[ SERVER ] 回應 " + socket.RemoteEndPoint.ToString()
												+ " (結果) 新增聊天室群組完成，房間名:" + instuction[2] + "\r\n";
									};
									this.Dispatcher.BeginInvoke(methodDelegate_showResult);
								}
								else
								{
									AsyncSend(dicClient[socket.RemoteEndPoint.ToString()], "ADD_CHATROOM_DENY");
									Action methodDelegate_showERR = delegate ()
									{
										LogTextBox.Text += "[ SERVER ] 回應 " + socket.RemoteEndPoint.ToString()
												+ " (結果) 新增聊天室群組失敗，已存在\r\n";
									};
									this.Dispatcher.BeginInvoke(methodDelegate_showERR);
								}
							}
							catch(Exception e)
							{
								AsyncSend(dicClient[socket.RemoteEndPoint.ToString()], "ADD_CHATROOM_DENY");
								Action methodDelegate_showERR = delegate ()
								{
									LogTextBox.Text += "[ SERVER ] 回應 " + socket.RemoteEndPoint.ToString()
											+ " (結果) 新增聊天室群組失敗，錯誤代碼:" + e.Message + "\r\n";
								};
								this.Dispatcher.BeginInvoke(methodDelegate_showERR);
							}
						}
						else if (instuction[0].Equals("JOIN_CHATROOM"))
						{
							try
							{
								bool joinPermit = false;
								foreach (Dictionary<string, Dictionary<string, string>> d in ChatRoomList)
								{
									foreach (KeyValuePair<string, Dictionary<string, string>> room in d)
									{
										if (room.Key.Equals(instuction[1]))
										{
											foreach(KeyValuePair<string,string> ingroup in room.Value)
											{
												if (!ingroup.Key.Equals(socket.RemoteEndPoint.ToString()))
												{
													AsyncSend(dicClient[ingroup.Key], "GROUP_MEMBER_JOIN:" + instuction[2]);
												}
											}
											room.Value.Add(socket.RemoteEndPoint.ToString(), instuction[2]);
											joinPermit = true;
											break;
										}
									}
								}

								if(joinPermit)
								{
									AsyncSend(dicClient[socket.RemoteEndPoint.ToString()], "ADD_CHATROOM_PERMIT:" + instuction[1]);
									Action methodDelegate_showResult = delegate ()
									{
										LogTextBox.Text += "[ SERVER ] 回應 " + socket.RemoteEndPoint.ToString()
												+ " (結果) 加入聊天室群組，房間名:" + instuction[1] + "\r\n";
									};
									this.Dispatcher.BeginInvoke(methodDelegate_showResult);
								}
								else
								{
									AsyncSend(dicClient[socket.RemoteEndPoint.ToString()], "JOIN_CHATROOM_DENY");
									Action methodDelegate_showResult = delegate ()
									{
										LogTextBox.Text += "[ SERVER ] 回應 " + socket.RemoteEndPoint.ToString()
												+ " (結果) 拒絕加入請求，聊天室已不存在\r\n";
									};
									this.Dispatcher.BeginInvoke(methodDelegate_showResult);
								}
							}
							catch (Exception e)
							{
								AsyncSend(dicClient[socket.RemoteEndPoint.ToString()], "ADD_CHATROOM_DENY");
								Action methodDelegate_showERR = delegate ()
								{
									LogTextBox.Text += "[ SERVER ] 回應 " + socket.RemoteEndPoint.ToString()
											+ " (結果) 加入聊天室群組失敗，錯誤代碼:" + e.Message + "\r\n";
								};
								this.Dispatcher.BeginInvoke(methodDelegate_showERR);
							}
						}
						else if (instuction[0].Equals("SEND"))
						{
							foreach (Dictionary<string,Dictionary<string,string>> d in ChatRoomList)
							{
								foreach(KeyValuePair<string,Dictionary<string,string>> room in d)
								{
									if(room.Key.Equals(instuction[1]))
									{
										foreach(KeyValuePair<string,string> member in room.Value)
										{
											if(member.Key.Equals(socket.RemoteEndPoint.ToString()))
											{
												AsyncSend(dicClient[member.Key], "ADD_CHAT_TO_OWN:" + instuction[2]);
											}
											else
											{
												AsyncSend(dicClient[member.Key], "ADD_CHAT:" +room.Value[socket.RemoteEndPoint.ToString()] +"/"+ instuction[2]);
											}
										}
									}
								}
							}
						}
						else if (instuction[0].Equals("EXIT_CHATROOM"))
						{
							try
							{
								string deletefrom = "";
								foreach (Dictionary<string, Dictionary<string, string>> d in ChatRoomList)
								{
									foreach (KeyValuePair<string, Dictionary<string, string>> room in d)
									{
										if (room.Value.ContainsKey(socket.RemoteEndPoint.ToString()))
										{
											string deleteUser = room.Value[socket.RemoteEndPoint.ToString()];
											foreach (KeyValuePair<string, string> ingroup in room.Value)
											{
												if (!ingroup.Key.Equals(socket.RemoteEndPoint.ToString()))
												{
													AsyncSend(dicClient[ingroup.Key], "GROUP_MEMBER_LEAVE:" + deleteUser);
												}
											}
											deletefrom = room.Key;
											room.Value.Remove(socket.RemoteEndPoint.ToString());
											break;
										}
									}
								}

								Dictionary<string, Dictionary<string, string>> tempD = new Dictionary<string, Dictionary<string, string>>();
								List<Dictionary<string, Dictionary<string, string>>> tempList = new List<Dictionary<string, Dictionary<string, string>>>();
								foreach (Dictionary<string, Dictionary<string, string>> d in ChatRoomList)
								{
									foreach (KeyValuePair<string, Dictionary<string, string>> eachChatRoom in d)
									{
										if (eachChatRoom.Value.Count > 0)
										{
											tempD.Add(eachChatRoom.Key, eachChatRoom.Value);
										}
									}
								}
								tempList.Add(tempD);

								this.ChatRoomList.Clear();
								this.ChatRoomList = tempList;

								string Room_info = "";
								if (tempD.Count>0)
								{
									foreach (Dictionary<string, Dictionary<string, string>> d in ChatRoomList)
									{
										foreach (KeyValuePair<string, Dictionary<string, string>> eachChatRoom in d)
										{
											Room_info += eachChatRoom.Key + "/" + eachChatRoom.Value.Count + "/";
										}
									}
									Room_info = Room_info.Substring(0, Room_info.Length - 1);
									AsyncSend(dicClient[socket.RemoteEndPoint.ToString()], "PREVIEW_CHATROOM_PERMIT:" + Room_info);
								}
								else
								{
									AsyncSend(dicClient[socket.RemoteEndPoint.ToString()], "PREVIEW_CHATROOM_PERMIT_NODATA");
								}

								Action methodDelegate_deleteMember = delegate ()
								{
									LogTextBox.Text += "[ SERVER ] 回應 " + socket.RemoteEndPoint.ToString()
											+ " (結果) 已自聊天室群組(" + deletefrom + ")剔除\r\n";
								};
								this.Dispatcher.BeginInvoke(methodDelegate_deleteMember);
							}
							catch(Exception e)
							{
								Action methodDelegate_deleteMember = delegate ()
								{
									LogTextBox.Text += "[ SERVER ] 回應 " + socket.RemoteEndPoint.ToString()
											+ " (例外狀況)" + e.Message + "\r\n";
								};
								this.Dispatcher.BeginInvoke(methodDelegate_deleteMember);
							}
						}
					}
					catch (Exception)
					{
						AsyncReceive(socket);
					}

					AsyncReceive(socket);
				}, null);

			}
			catch (Exception e)
			{
				//傳送失敗，將該客戶端資訊刪除
				string deleteClient = socket.RemoteEndPoint.ToString();
				dicClient.Remove(deleteClient);
				//使用Dispatcher物件跨執行緒
				Action methodDelegate = delegate ()
				{
					LogTextBox.Text += "[ " + deleteClient + " ] 已離線\r\n";
					LogTextBox.Text += "[ SERVER ] 回應 " + deleteClient + " 自列表移除用戶: " + deleteClient + "\r\n";
					ClientComboBox.Items.Remove(deleteClient);
				};
				this.Dispatcher.BeginInvoke(methodDelegate);
				//MessageBox.Show("AsyncReceive: "+e.Message);
			}
		}

		/// <summary>
		/// 傳送訊息
		/// </summary>
		/// <param name="client"></param>
		/// <param name="p"></param>
		private void AsyncSend(Socket client, string message)
		{
			if (client == null || message == string.Empty) return;
			//資料轉碼
			byte[] data = Encoding.UTF8.GetBytes(message);
			try
			{
				//開始傳送訊息
				client.BeginSend(data, 0, data.Length, SocketFlags.None, asyncResult =>
				{
					//完成訊息傳送
					int length = client.EndSend(asyncResult);
				}, null);
			}
			catch (Exception e)
			{
				//傳送失敗，將該客戶端資訊刪除
				string deleteClient = client.RemoteEndPoint.ToString();
				dicClient.Remove(deleteClient);
				ClientComboBox.Items.Remove(deleteClient);
				MessageBox.Show("AsyncSend: "+e.Message,"Server ERR");
			}
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			if (ClientComboBox.SelectedIndex == -1)
			{
				AsyncSend(socketConnect, SendTextBox.Text);
			}
			else
			{
				AsyncSend(dicClient[ClientComboBox.SelectedItem.ToString()], SendTextBox.Text);
			}
		}

		private void ConnectBTN_Click(object sender, RoutedEventArgs e)
		{
			//OpenServer();
			try
			{
				using (MySqlConnection conn = new MySqlConnection(conn_info))
				{
					conn.Open();
					ConnectBTN.IsEnabled = false;
					LoadingBTN.Visibility = Visibility.Visible;
					worker.RunWorkerAsync();
					string tryCMD = "SELECT * FROM user";
					MySqlCommand cmd = new MySqlCommand(tryCMD, conn);
					MySqlDataReader dataRead = cmd.ExecuteReader();

					if (dataRead.HasRows)
					{
						checkDatabaseConnect = true;
						/*Action methodDelegate_VerifyId = delegate ()
						{
							LogTextBox.Text += "[ SERVER ] 連線至 Azure 線上資料庫成功.\r\n";
							/*while (dataRead.Read())
							{
								LogTextBox.Text += "[ SERVER ] Try User ID check:" + dataRead["User_ID"] + "\r\n";
							}
						};
						this.Dispatcher.BeginInvoke(methodDelegate_VerifyId);*/
					}
					else
					{
						checkDatabaseConnect = false;
						/*Action methodDelegate_VerifyId = delegate ()
						{
							LogTextBox.Text += "[ SERVER ] (錯誤報告)連線至線上資料庫失敗.\r\n";
						};
						this.Dispatcher.BeginInvoke(methodDelegate_VerifyId);*/
					}
				}
			}
			catch (MySql.Data.MySqlClient.MySqlException ex)
			{
				checkDatabaseConnect = false;
				switch (ex.Number)
				{
					case 0:
						LogTextBox.Text += "[ SERVER ] Unpredicted incident occured. Fail to connect to database.\r\n";
						break;
					case 1042:
						LogTextBox.Text += "[ SERVER ] IP error. Please check again.\r\n";
						break;
					case 1045:
						LogTextBox.Text += "[ SERVER ] User account or password error. Please check again\r\n";
						break;
				}
			}
		}

		private void ClearBTN_Click(object sender, RoutedEventArgs e)
		{
			LogTextBox.Text = "";
		}
		/*private void setText(string str)
{
	/*if (this.InvokeRequired)
	{
		this.Invoke(new MethodInvoker(() => setText(str)));
	}
	else
	{
		EventTextBox.Text += "\r\n" + str;
	}
}*/
	}
}
