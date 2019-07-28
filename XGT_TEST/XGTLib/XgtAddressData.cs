using System;
using System.Collections.Generic;
using System.Text;

namespace XGTLib
{
    public class XgtAddressData
    {
        public string Address { get; set; }
        public string Data { get; set; }
        public byte[] DataByteArray { get; set; }
        /// <summary>
        /// 주소 문자열 표현, EX) %DW1100
        /// </summary>
        public string AddressString { get; set; }
        /// <summary>
        /// AddressString 을 바이트 배열로 변환
        /// </summary>
        public byte[] AddressByteArray
        {
            get
            {
                byte[] value = Encoding.ASCII.GetBytes(AddressString);
                return value;
            }            
        }
        /// <summary>
        /// AddressByteArray 바이트 배열의 수(2byte)
        /// </summary>
        public byte[] LengthByteArray
        {
            get
            {
                byte[] value = BitConverter.GetBytes((short)AddressByteArray.Length);
                return value;
            }

        }
    }
}
