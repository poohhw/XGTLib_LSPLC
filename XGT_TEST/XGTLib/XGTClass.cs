using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace XGTLib
{
    public enum XGT_Request_Func
    {
        Read = 84,  //0x54
        ReadResponse = 85, //0x55
        Write = 88,  //0x58
        WriteResponse = 89 //0x59
    }
    public enum XGT_DataType
    {
        Bit = 0x0000,
        Byte = 0x0001,
        Word = 0x0002,
        DWord = 0x0003,
        LWord = 0x0004,
        Continue = 0x0014
    }
    public enum XGT_MemoryType
    {
        /// <summary>입출력(Bit)</summary>
        IO = 0,
        /// <summary>보조릴레이(Bit)</summary>
        SubRelay = 1,
        /// <summary>링크릴레이(Bit)</summary>
        LinkRelay = 2,
        /// <summary>Keep릴레이(Bit)</summary>
        KeepRelay = 3,
        /// <summary>특수릴레이(Bit)</summary>
        EtcRelay = 4,
        /// <summary>타이머(현재값)(Word)</summary>
        Timer = 5,
        /// <summary>카운터(현재값)(Word)</summary>
        Counter = 6,
        // <summary>데이터레지스터(Word)</summary>
        DataRegister = 7,
        /// <summary>통신 데이터레지스터(Word)</summary>
        ComDataRegister = 8,
        /// <summary>파일 레지스터(Word)</summary>
        FileDataRegister = 9,

    }

    public static class XGT_Data_TypeClass
    {
        public const string Bit = "X";
        public const string Byte = "B";
        public const string Word = "W";
        public const string DWord = "D";
        public const string LWord = "L";
    }

    public static class XGT_Memory_TypeClass
    {
        /// <summary>입출력(Bit)</summary>
        public const string IO = "P";
        /// <summary>보조릴레이(Bit)</summary>
        public const string SubRelay = "M";
        /// <summary>링크릴레이(Bit)</summary>
        public const string LinkRelay = "L";
        /// <summary>Keep릴레이(Bit)</summary>
        public const string KeepRelay = "K";
        /// <summary>특수릴레이(Bit)</summary>
        public const string EtcRelay = "F";
        /// <summary>타이머(현재값)(Word)</summary>
        public const string Timer = "T";
        /// <summary>카운터(현재값)(Word)</summary>
        public const string Counter = "C";
        /// <summary>데이터레지스터(Word)</summary>
        public const string DataRegister = "D";
        /// <summary>통신 데이터레지스터(Word)</summary>
        public const string ComDataRegister = "N";
        /// <summary>파일 레지스터(Word)</summary>
        public const string FileDataRegister = "R";
    }


    public class XGTClass
    {
        private const string _companyID = "LSIS-XGT";
        //접속 상태 : true , false
        private bool _connected;
        private string _ip;
        private int _port;
        private Socket tcpSocket;
        private byte[] tcpSocketBuffer = new byte[256];

        private int _timeout;



        /// <summary>
        /// 접속 상태
        /// </summary>
        public bool Connected
        {
            get { return _connected; }
        }

        public XGTClass(string pIP, int pPort, int pTimeout = 3000)
        {
            //생성자
            _ip = pIP;
            _port = pPort;
            _connected = false;
            _timeout = pTimeout;

            if (tcpSocket != null) tcpSocket.Dispose();
        }

        /// <summary>
        /// 연결하기
        /// </summary>
        /// <returns></returns>
        public bool Connect()
        {
            try
            {
                IPAddress ipaddr;

                if (IPAddress.TryParse(_ip, out ipaddr))
                {
                    //IPHostEntry hst = Dns.GetHostEntry(_ip);
                    //_ip = hst.AddressList[0].ToString();

                    tcpSocket = new Socket(IPAddress.Parse(_ip).AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    tcpSocket.Connect(new IPEndPoint(IPAddress.Parse(_ip), _port));
                    tcpSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, _timeout);
                    tcpSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, _timeout);
                    tcpSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.NoDelay, 1);
                }

                _connected = tcpSocket.Connected;

                // IPAddress 
            }
            catch (Exception ex)
            {
                _connected = false;                
            }

            return _connected;
        }

        /// <summary>
        /// 접속종료
        /// </summary>
        public void Disconnect()
        {
            if (tcpSocket != null)
            {
                tcpSocket.Disconnect(false);
                tcpSocket.Dispose();
            }
            this.Dispose();
        }

        private void Dispose()
        {
            if (tcpSocket != null)
            {
                if (tcpSocket.Connected)
                {
                    try { tcpSocket.Shutdown(SocketShutdown.Both); }
                    catch { }

                    tcpSocket.Close();
                    _connected = tcpSocket.Connected;
                }
                tcpSocket = null;
            }
        }

        /// <summary>
        /// 데이터 읽기
        /// </summary>
        /// <param name="pDataType">연속읽기인 경우 (pDataCount)데이터 갯수를 꼭 입력해야함.</param>
        /// <param name="pAddress">메모리주소 번지</param>
        /// <param name="pMemtype">메모리 타입</param>
        /// <param name="pInvokeID"></param>
        /// <param name="pMsg">리턴 메세지</param>
        /// <param name="pDataCount">연속읽기일 경우 읽을 데이터 갯수, 개별 읽기면 기본값 0</param>
        /// <param name="pInputData">쓰기일 경우 입력데이터 받아옴.</param>
        /// <returns></returns>
        public XGTData Read(XGT_DataType pDataType, List<XgtAddressData> pAddress, XGT_MemoryType pMemtype, int pInvokeID, int pDataCount = 0)
        {
            XGTData vData = new XGTData();

            if (pAddress.Count > 16)
            {
                vData.Message = "16개 이상 읽어올 수 없습니다.";
            }
            else
            {
                var buffer = new List<byte>();

                try
                {
                    byte[] data = CreateReadDataFormat(XGT_Request_Func.Read, pDataType, pAddress, pMemtype, pDataCount);
                    byte[] header = CreateHeader(pInvokeID, data.Length);

                    //전송할 프레임
                    byte[] tcpFrame = new byte[header.Length + data.Length];

                    //어플레케이션 헤더와 데이터 정보를 합쳐서 전송 Frame을 만든다.
                    int idx = 0;

                    AddByte(header, ref idx, ref tcpFrame);
                    AddByte(data, ref idx, ref tcpFrame);

                    vData.TX = tcpFrame;

                    if (tcpSocket == null || !tcpSocket.Connected)
                    {
                        tcpSocket.Connect(_ip, _port);
                    }
                    tcpSocket.Send(tcpFrame, 0, tcpFrame.Length, SocketFlags.None);

                    do
                    {
                        byte[] currByte = new byte[1];
                        int byteCounter = tcpSocket.Receive(currByte, 0, currByte.Length, SocketFlags.None);

                        if (byteCounter.Equals(1))
                        {
                            buffer.Add(currByte[0]);
                        }
                    } while (tcpSocket.Available > 0);

                    vData.RX = buffer.ToArray();
                    vData.Message = "OK";                   

                }
                catch (Exception ex)
                {
                    vData.Message = "ERROR:" + ex.Message.ToString();
                }
                finally
                {
                    vData.MakeData();
                }

            }
            return vData;
        }

        public XGTData Write(XGT_DataType pDataType, List<XgtAddressData> pAddressList, XGT_MemoryType pMemtype, int pInvokeID, int pDataCount = 0)
        {
            var buffer = new List<byte>();
            XGTData vData = new XGTData();
            try
            {
                byte[] data = CreateWriteDataFormat(XGT_Request_Func.Write, pDataType, pAddressList, pMemtype,pDataCount);
                byte[] header = CreateHeader(pInvokeID, data.Length);


                //전송할 프레임
                byte[] tcpFrame = new byte[header.Length + data.Length];

                //어플레케이션 헤더와 데이터 정보를 합쳐서 전송 Frame을 만든다.
                int idx = 0;

                AddByte(header, ref idx, ref tcpFrame);
                AddByte(data, ref idx, ref tcpFrame);


                vData.TX = tcpFrame;

                if (tcpSocket == null || !tcpSocket.Connected)
                {
                    tcpSocket.Connect(_ip, _port);
                }
                tcpSocket.Send(tcpFrame, 0, tcpFrame.Length, SocketFlags.None);

                do
                {
                    byte[] currByte = new byte[1];
                    int byteCounter = tcpSocket.Receive(currByte, 0, currByte.Length, SocketFlags.None);

                    if (byteCounter.Equals(1))
                    {
                        buffer.Add(currByte[0]);
                    }
                } while (tcpSocket.Available > 0);

                vData.RX = buffer.ToArray();
                vData.Message = "OK";
            }
            catch (Exception ex)
            {
                vData.Message = "Error:" + ex.Message.ToString();
            }
            finally
            {
                vData.MakeData();
            }
            return vData;
        }


        public byte[] Read2()  //연속읽기 테스트
        {
            byte[] value = Encoding.ASCII.GetBytes("%DB3400");

            var a = BitConverter.ToString(value);

            byte[] testSend = new byte[39];


            byte[] CompanyID = Encoding.ASCII.GetBytes(_companyID);  //Company ID (8 Byte)


            string cm = Encoding.ASCII.GetString(CompanyID);  //Company ID (8 Byte)
            //Array.Reverse(CompanyID);

            //헤더부분
            int idx = 0;
            AddByte(CompanyID, ref idx, ref testSend);

            //예약영역
            testSend[8] = 0x00;
            testSend[9] = 0x00;

            //PLC정보 영역
            testSend[10] = 0x00;
            testSend[11] = 0x00;

            //CPU 정보
            testSend[12] = 0xA0;
            //H33
            testSend[13] = 0x33;

            //INVOKE ID
            testSend[14] = 0x00;//BitConverter.GetBytes(17)[1];
            testSend[15] = 0x00;//BitConverter.GetBytes(17)[0];

            //LENGTH
            testSend[16] = 0x13;
            testSend[17] = 0x00;

            //POSITION
            testSend[18] = 0x00;  //슬롯번호 00, 베이스번호 01? 00?
            //BCC
            testSend[19] = 0x00;


            //데이터 부분
            //명령어
            testSend[20] = 0x54;
            testSend[21] = 0x00;
            //데이터 타입
            testSend[22] = 0x14;
            testSend[23] = 0x00;
            //예약영역
            testSend[24] = 0x00;
            testSend[25] = 0x00;
            //블록수
            testSend[26] = 0x01;
            testSend[27] = 0x00;
            //변수길이
            testSend[28] = 0x07;
            testSend[29] = 0x00;

            //직접변수

            byte[] count = new byte[2];
            count[0] = 0x06;
            count[1] = 0x00;

            idx = 30;

            AddByte(value, ref idx, ref testSend);
            AddByte(count, ref idx, ref testSend);

            string a1 = BitConverter.ToString(testSend);
            a1 = a1.Replace("-", "");

            if (tcpSocket == null || !tcpSocket.Connected)
            {
                tcpSocket.Connect(_ip, _port);
            }
            tcpSocket.Send(testSend, 0, testSend.Length, SocketFlags.None);

            int result = tcpSocket.Receive(tcpSocketBuffer, 0, tcpSocketBuffer.Length, SocketFlags.None);

            return tcpSocketBuffer;
        }
        public byte[] Read1()  //개별읽기 테스트
        {
            byte[] value = Encoding.ASCII.GetBytes("%DW1100");

            var a = BitConverter.ToString(value);

            byte[] testSend = new byte[37];


            byte[] CompanyID = Encoding.ASCII.GetBytes(_companyID);  //Company ID (8 Byte)


            string cm = Encoding.ASCII.GetString(CompanyID);  //Company ID (8 Byte)
            //Array.Reverse(CompanyID);

            //헤더부분
            int idx = 0;
            AddByte(CompanyID, ref idx, ref testSend);

            //예약영역
            testSend[8] = 0x00;
            testSend[9] = 0x00;

            //PLC정보 영역
            testSend[10] = 0x00;
            testSend[11] = 0x00;

            //CPU 정보
            testSend[12] = 0xA0;
            //H33
            testSend[13] = 0x33;

            //INVOKE ID
            testSend[14] = 0x00;//BitConverter.GetBytes(17)[1];
            testSend[15] = 0x00;//BitConverter.GetBytes(17)[0];

            //LENGTH
            testSend[16] = 0x11;
            testSend[17] = 0x00;

            //POSITION
            testSend[18] = 0x00;  //슬롯번호 00, 베이스번호 01? 00?
            //BCC
            testSend[19] = 0x00;


            //데이터 부분
            //명령어
            testSend[20] = 0x54;
            testSend[21] = 0x00;
            //데이터 타입
            testSend[22] = 0x02;
            testSend[23] = 0x00;
            //예약영역
            testSend[24] = 0x00;
            testSend[25] = 0x00;
            //블록수
            testSend[26] = 0x01;
            testSend[27] = 0x00;
            //변수길이
            testSend[28] = 0x07;
            testSend[29] = 0x00;

            //직접변수

            idx = 30;

            AddByte(value, ref idx, ref testSend);

            string a1 = BitConverter.ToString(testSend);
            a1 = a1.Replace("-", "");

            if (tcpSocket == null || !tcpSocket.Connected)
            {
                tcpSocket.Connect(_ip, _port);
            }
            tcpSocket.Send(testSend, 0, testSend.Length, SocketFlags.None);

            int result = tcpSocket.Receive(tcpSocketBuffer, 0, tcpSocketBuffer.Length, SocketFlags.None);

            return tcpSocketBuffer;
        }

        //어플리케이션 헤더 만들기
        public byte[] CreateHeader(int pInvokeID, int pDataByteLenth)
        {

            byte[] CompanyID = Encoding.ASCII.GetBytes(_companyID);  //Company ID (8 Byte)
            byte[] Reserved = BitConverter.GetBytes((short)0);  //Reserved 예약영역  2 Byte (고정값  value is fix)
            byte[] PLCInfo = BitConverter.GetBytes((short)0); // PLC Info >> Client 0x00;
            byte[] CPUInfo = new byte[1];
            CPUInfo[0] = 0xA0;            //CPU INFO 1 Byte
            byte[] SOF = new byte[1];
            SOF[0] = 0x33;                //Source of Frame ( 고정값 value is fix)
            byte[] InvokeID = BitConverter.GetBytes((short)pInvokeID);

            byte[] Length = BitConverter.GetBytes((short)pDataByteLenth); //Application Data Format 바이트 크기
            byte[] FEnetPosition = new byte[1];
            FEnetPosition[0] = 0x00;      //Bit0~3 : 이더넷 모듈의 슬롯 번호 ,  Bit4~7 : 이더넷 모듈의 베이스 번호
            byte[] Reserved2 = new byte[1];
            Reserved2[0] = 0x00;

            //헤더 프레임의 길이 계산.
            int vLenth = CompanyID.Length + Reserved.Length + PLCInfo.Length + CPUInfo.Length + SOF.Length
                                  + InvokeID.Length + Length.Length + FEnetPosition.Length + Reserved2.Length;

            byte[] header = new byte[vLenth];

            int idx = 0;
            AddByte(CompanyID, ref idx, ref header);
            AddByte(Reserved, ref idx, ref header);
            AddByte(PLCInfo, ref idx, ref header);
            AddByte(CPUInfo, ref idx, ref header);
            AddByte(SOF, ref idx, ref header);
            AddByte(InvokeID, ref idx, ref header);
            AddByte(Length, ref idx, ref header);
            AddByte(FEnetPosition, ref idx, ref header);
            AddByte(Reserved2, ref idx, ref header);


            return header;
        }



        //어플리케이션 데이터 READ 포맷 만들기
        private byte[] CreateReadDataFormat
            (XGT_Request_Func emFunc, XGT_DataType emDatatype, List<XgtAddressData> pAddressList, XGT_MemoryType emMemtype, int pDataCount)
        {
            List<XgtAddressData> lstAddress = new List<XgtAddressData>();
            int vLenth = 0;  //데이타 포맷 프레임의 크기

            byte[] command = BitConverter.GetBytes((short)emFunc); //StringToByteArray((int)emFunc, true);  //명령어 읽기,쓰기
            byte[] dataType = BitConverter.GetBytes((short)emDatatype);//StringToByteArray((int)emDatatype, true);  //데이터 타입

            byte[] reserved = BitConverter.GetBytes((short)0);  //예약영역 고정(0x0000)
            byte[] blockcount = BitConverter.GetBytes((short)pAddressList.Count); //블록수 

            //프레임 크기 설정 :  명령어(2) + 데이터타입(2) + 예약영역(2) + 블록수 (?) + 변수길이(?) + 변수(?)
            vLenth = command.Length + dataType.Length + reserved.Length + blockcount.Length;

            foreach (XgtAddressData addr in pAddressList)
            {
                string vAddress = CreateValueName(emDatatype, emMemtype, addr.Address);

                //byte[] value = Encoding.ASCII.GetBytes(vAddress);
                //byte[] valueLength = BitConverter.GetBytes((short)value.Length);

                XgtAddressData XgtAddr = new XgtAddressData();
                XgtAddr.AddressString = vAddress;

                lstAddress.Add(XgtAddr);


                vLenth += XgtAddr.AddressByteArray.Length + XgtAddr.LengthByteArray.Length;
            }

            if (XGT_DataType.Continue == emDatatype && XGT_Request_Func.Read == emFunc)
            {
                vLenth += 2;  //연속읽기 인 경우 2바이트 추가.(데이터 갯수)
            }

            byte[] data = new byte[vLenth];


            int idx = 0;
            AddByte(command, ref idx, ref data);
            AddByte(dataType, ref idx, ref data);
            AddByte(reserved, ref idx, ref data);
            AddByte(blockcount, ref idx, ref data);

            foreach (XgtAddressData addr in lstAddress)
            {
                AddByte(addr.LengthByteArray, ref idx, ref data);
                AddByte(addr.AddressByteArray, ref idx, ref data);
            }

            /* 연속 읽기의 경우 읽을 갯수 지정. */
            if (XGT_DataType.Continue == emDatatype)
            {
                //데이터 타입이 연속 읽기 인 경우.
                byte[] vDataCount = BitConverter.GetBytes((short)pDataCount);
                AddByte(vDataCount, ref idx, ref data);
            }


            return data;
        }
        //어플리케이션 데이터 WRITE 포맷 만들기
        private byte[] CreateWriteDataFormat
            (XGT_Request_Func emFunc, XGT_DataType emDatatype, List<XgtAddressData> pAddressList, XGT_MemoryType emMemtype, int pDataCount)
        {
            
            int vLenth = 0;  //데이타 포맷 프레임의 크기

            byte[] command = BitConverter.GetBytes((short)emFunc);     //StringToByteArray((int)emFunc, true);  //명령어 읽기,쓰기
            byte[] dataType = BitConverter.GetBytes((short)emDatatype);//StringToByteArray((int)emDatatype, true);  //데이터 타입

            byte[] reserved = BitConverter.GetBytes((short)0);  //예약영역 고정(0x0000)
            byte[] blockcount = BitConverter.GetBytes((short)pAddressList.Count); //블록수 

            //프레임 크기 설정 :  명령어(2) + 데이터타입(2) + 예약영역(2) + 블록수 (?) + 변수길이(?) + 변수(?)
            vLenth = command.Length + dataType.Length + reserved.Length + blockcount.Length;

            List<XgtAddressData> lstAddress = new List<XgtAddressData>();

            foreach (XgtAddressData addr in pAddressList)
            {
                string vAddress = CreateValueName(emDatatype, emMemtype, addr.Address);
                                
                addr.AddressString = vAddress;      
                                            

                object oData = new object(); //입력받은 값이 숫자형인지 문자형이지 확실치 않아 Object 로 선언
                int oDataLength = 0;         //입력받은 값의 바이트 배열의 크기.

                //데이터 쓰기일 경우 입력 데이터의 크기를 구한다.
                int nInput = 0;    //입력받은 데이터가 숫자형일경우 받을 변수
                string strInput = string.Empty;  //입력받은 데이터가 문자형일 경우 받을 변수.

                if (!int.TryParse(addr.Data, out nInput))
                {
                    //문자형일 경우
                    strInput = addr.Data;
                    oData = Encoding.ASCII.GetBytes(strInput);
                }
                else
                {
                    //숫자형일 경우
                    oData = BitConverter.GetBytes((short)nInput);
                    
                }

                if (emDatatype == XGT_DataType.Bit)
                {
                    addr.DataByteArray = new byte[1];
                    addr.DataByteArray[0] = ((byte[])oData)[0];
                }
                else
                {
                    addr.DataByteArray = (byte[])oData;
                }

                //입력값의 바이트 배열의 크기
                oDataLength = ((byte[])oData).Length;

                vLenth += addr.AddressByteArray.Length + addr.LengthByteArray.Length + 2 + oDataLength; //데이터 갯수 + 데이터 길이

                lstAddress.Add(addr);
            }

            if (XGT_DataType.Continue == emDatatype)
            {
                vLenth += 2;  //연속읽기 인 경우 2바이트 추가.(데이터 갯수)
            }

            byte[] data = new byte[vLenth];

               
            int idx = 0;
            AddByte(command, ref idx, ref data);
            AddByte(dataType, ref idx, ref data);
            AddByte(reserved, ref idx, ref data);
            AddByte(blockcount, ref idx, ref data);
                       

            foreach (XgtAddressData addr in lstAddress)
            {
                AddByte(addr.LengthByteArray, ref idx, ref data);
                AddByte(addr.AddressByteArray, ref idx, ref data);
            }

       

            foreach (XgtAddressData addr in lstAddress)
            {
                //데이터 쓰기일 경우
                byte[] count = BitConverter.GetBytes((short)addr.DataByteArray.Length);

                AddByte(count, ref idx, ref data);
                AddByte(addr.DataByteArray, ref idx, ref data);
            }


            return data;
        }

        /// <summary>
        /// 메모리 어드레스 변수이름을 생성한다.
        /// </summary>
        /// <param name="dataType">데이터타입</param>
        /// <param name="memType">메모리타입</param>
        /// <param name="pAddress">주소번지</param>
        /// <returns></returns>
        private string CreateValueName(XGT_DataType dataType, XGT_MemoryType memType, string pAddress)
        {
            string vReturn = string.Empty;

            string vMemTypeChar = this.GetMemTypeChar(memType); //메모리타입
            string vDataTypeChar = this.GetTypeChar(dataType);  //데이터타입


            if (dataType == XGT_DataType.Continue)
            {
                //연속읽기의 경우는 바이트 단위로만 표현 할 수 있으므로, Word 단위의 메모리를 읽으려면  주소값 * 2 를 해야함.
                //2Byte = 1Word  
                pAddress = (Convert.ToInt32(pAddress) * 2).ToString();
            }

            if(dataType == XGT_DataType.Bit)
            {
                /*
                    변수이름 표현 방법에 있어서 비트 영역을 접근하고자 할 때에는 메모리 디바이스의 데이터
                    타입 단위의 순서로 표현하여야 합니다. M172의 C번째 비트를 쓰기 위하여서는 M이
                    WORD디바이스 이므로 아래와 같이 비트 타입으로 산출해내는 과정이 필요합니다.
                    잘못된 표현: %MX172C
                    올바른 표현: 172 x 16(WORD) + 12(BIT) = 2764
                     %MX2764
                 */
                int vSEQ = 0;
                string vAddress = pAddress.Substring(0, pAddress.Length - 1);  //입력받은 주소값의 마지막 자리 앞까지가 주소번지
                string Last = pAddress.Substring(pAddress.Length -1);  // 입력받은 주소값의 마지막 자리는 비트 위치
                vSEQ = Convert.ToInt32(Last, 16);
                pAddress = (Convert.ToInt32(vAddress) * 16 + vSEQ).ToString();
            }


            return $"%{vMemTypeChar}{vDataTypeChar}{pAddress}";
        }

        /// <summary>
        /// 데이터 형식에 따른 Char 반환
        /// </summary>
        /// <param name="type">데이터타입</param>
        /// <returns></returns>
        private string GetTypeChar(XGT_DataType type)
        {
            string vReturn = string.Empty; // 기본값은  Bit

            switch (type)
            {
                case XGT_DataType.Bit:
                    vReturn = XGT_Data_TypeClass.Bit;
                    break;
                case XGT_DataType.Byte:
                    vReturn = XGT_Data_TypeClass.Byte;
                    break;
                case XGT_DataType.Word:
                    vReturn = XGT_Data_TypeClass.Word;
                    break;
                case XGT_DataType.DWord:
                    vReturn = XGT_Data_TypeClass.DWord;
                    break;
                case XGT_DataType.LWord:
                    vReturn = XGT_Data_TypeClass.LWord;
                    break;
                case XGT_DataType.Continue:  // 연속읽기에는 ByteType만... 
                    vReturn = XGT_Data_TypeClass.Byte;
                    break;
                default:
                    vReturn = XGT_Data_TypeClass.Bit; ;
                    break;
            }

            return vReturn;
        }

        /// <summary>
        /// 메모리 타입에에 따른 Char 반환
        /// </summary>
        /// <param name="type">메모리타입</param>
        /// <returns></returns>
        private string GetMemTypeChar(XGT_MemoryType type)
        {
            string vReturn = string.Empty;
            switch (type)
            {
                case XGT_MemoryType.IO:
                    vReturn = XGT_Memory_TypeClass.IO;
                    break;
                case XGT_MemoryType.SubRelay:
                    vReturn = XGT_Memory_TypeClass.SubRelay;
                    break;
                case XGT_MemoryType.LinkRelay:
                    vReturn = XGT_Memory_TypeClass.LinkRelay;
                    break;
                case XGT_MemoryType.KeepRelay:
                    vReturn = XGT_Memory_TypeClass.KeepRelay;
                    break;
                case XGT_MemoryType.EtcRelay:
                    vReturn = XGT_Memory_TypeClass.EtcRelay;
                    break;
                case XGT_MemoryType.Timer:
                    vReturn = XGT_Memory_TypeClass.Timer;
                    break;
                case XGT_MemoryType.DataRegister:
                    vReturn = XGT_Memory_TypeClass.DataRegister;
                    break;
                case XGT_MemoryType.Counter:
                    vReturn = XGT_Memory_TypeClass.Counter;
                    break;
                case XGT_MemoryType.ComDataRegister:
                    vReturn = XGT_Memory_TypeClass.ComDataRegister;
                    break;
                case XGT_MemoryType.FileDataRegister:
                    vReturn = XGT_Memory_TypeClass.FileDataRegister;
                    break;
            }

            return vReturn;
        }




        /// <summary>
        /// 바이트 합치기
        /// </summary>
        /// <param name="item">개별바이트</param>
        /// <param name="idx">전체바이트에 개별바이트를 합칠 인덱스</param>
        /// <param name="header">전체바이트</param>
        /// <returns>전체 바이트 </returns>
        private byte[] AddByte(byte[] item, ref int idx, ref byte[] header)
        {
            Array.Copy(item, 0, header, idx, item.Length);
            idx += item.Length;

            return header;
        }


    }
}

