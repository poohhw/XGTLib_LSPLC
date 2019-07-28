using System;
using System.Collections.Generic;
using System.Text;

namespace XGTLib
{
    public class XGTData
    {
        /// <summary>
        /// 송신프레임
        /// </summary>
        public byte[] TX { get; set; }
        public string TXstring
        {
            get { return TX == null ? "" : BitConverter.ToString(TX); }
        }
        /// <summary>
        /// 수신프레임
        /// </summary>
        public byte[] RX { get; set; }
        public string RXstring
        {
            get { return RX == null ? "" : BitConverter.ToString(RX); }
        }

        public List<XgtAddressData> DataList { get; set; }
        /// <summary>
        /// ACK응답 프레임 여부(데이터 정상 수신) : true;  데이터 비정상 수신 : false;
        /// </summary>
        public string ResponseStatus { get; set; }

        /// <summary>
        /// NAK 프레임 수신시 에러 메시지.
        /// </summary>
        public string NAK_ErrorCotent { get; set; }

        /// <summary>
        /// ACK 응답의 데이터 블럭 수.
        /// </summary>
        public int BlockCount { get; set; }
        /// <summary>
        /// 프레임 에러와 관계없음. 문법오류 및 런타임 Exception 일 경우 Error, 정상 응답인 경우 :OK
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 응답 메세지 타입
        /// </summary>
        public XGT_Request_Func ResponseType { get; set; }

        public XGT_DataType DataType { get; set; }

        /// <summary>
        /// 받아온 응답 값으로 데이터 분서 메서드.
        /// Read,Write 함수의 finally 구문에서 호출 하도록 작성하시오.
        /// </summary>
        public void MakeData()
        {
            try
            {
                NAK_ErrorCotent = string.Empty;

                List<XgtAddressData> lstData = new List<XgtAddressData>();

                //RX 응답 중 19번째가지는 헤더프레임 정보, 20번째부터 데이터 프레임.
                //받은 응답이 없으면, 즉 에러가 발생시 
                if (RX.Length == 0)
                {
                    NAK_ErrorCotent = "서버로 부터 응답을 받지 못했습니다.";
                    return;
                }
                if (RX[20] == (short)XGT_Request_Func.ReadResponse)
                {
                    ResponseType = XGT_Request_Func.ReadResponse;
                }
                if (RX[20] == (short)XGT_Request_Func.WriteResponse)
                {
                    ResponseType = XGT_Request_Func.WriteResponse;
                }

                byte[] vdataType = new byte[2];
                vdataType[0] = RX[22];
                vdataType[1] = RX[23];


                foreach (XGT_DataType item in Enum.GetValues(typeof(XGT_DataType)))
                {
                    string vb = BitConverter.ToString(BitConverter.GetBytes((short)item));
                    string va = BitConverter.ToString(vdataType);
                    if (vb.Equals(va))
                    {
                        DataType = item;
                        break;
                    }
                }


                if (RX[26] != 0x00 || RX[27] != 0x00)
                {
                    //에러응답
                    ResponseStatus = "NAK";
                    DataList = lstData;
                    //에러메세지 확인
                    switch (RX[28])
                    {
                        case 0x12:
                            NAK_ErrorCotent = "(0x12)연속읽기인데 바이트 타입이 아닌 경우";
                            break;
                        case 0x11:
                            NAK_ErrorCotent = "(0x11)변수명이 4보다 작거나 16보다 큰 경우와 같이 어드레스에 관련된 에러";
                            break;
                        case 0x10:
                            NAK_ErrorCotent = "(0x10)없는 디바이스를 요청하는 경우와 같이 디바이스에 관련된 에러";
                            break;
                        case 0x78:
                            NAK_ErrorCotent = "(0x78)unknown command";
                            break;
                        case 0x77:
                            NAK_ErrorCotent = "(0x77)체크섬 오류";
                            break;
                        case 0x76:
                            NAK_ErrorCotent = "(0x76)length 정보 오류";
                            break;
                        case 0x75:
                            NAK_ErrorCotent = "(0x75) “LGIS-GLOFA”가 아니거나 “LSIS-XGT”가 아닌 경우";
                            break;
                        case 0x24:
                            NAK_ErrorCotent = "(0x24)데이터 타입 에러";
                            break;
                        default:
                            NAK_ErrorCotent = "알려지지 않은 에러코드, LS산전 고객센터에 문의";
                            break;

                    }
                }
                else
                {
                    //28번 index 부터 데이터로 정의
                    int index = 28;

                    //정상응답
                    ResponseStatus = "ACK";
                    byte[] blockCount = new byte[2];  //블럭카운터
                    byte[] dataByteCount = new byte[2];  //데이터 크기
                    byte[] data = new byte[2];  //블럭카운터

                    Array.Copy(RX, index, blockCount, 0, 2);
                    BlockCount = BitConverter.ToInt16(blockCount, 0);

                    index = index + 2;

                    //블럭카운터 만큼의 데이터 갯수가 존재한다.

                    //Read일 경우 데이터 생성
                    if (ResponseType == XGT_Request_Func.ReadResponse)
                    {
                        for (int i = 0; i < BlockCount; i++)
                        {
                            Array.Copy(RX, index, dataByteCount, 0, 2);
                            int biteSize = BitConverter.ToInt16(dataByteCount, 0); //데이터 크기.

                            index = index + 2;

                            Array.Copy(RX, index, data, 0, biteSize);

                            index = index + biteSize;  //다음 인덱스 

                            string dataContent = BitConverter.ToString(data, 0);

                            XgtAddressData dataValue = new XgtAddressData();
                            dataValue.Data = dataContent;
                            dataValue.DataByteArray = data;

                            lstData.Add(dataValue);

                        }
                    }
                    DataList = lstData;

                }
            }
            catch(Exception ex)
            {
                
                Message = "Error: " + ex.Message.ToString(); 
            }


        }
    }
}
