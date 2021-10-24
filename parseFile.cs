using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp1
{
    public struct decodedLine
    {
        public bool valid;
        public string duration;
        public string decodedData;
    }

    
    public class ParseFile
    {
        public const string AUX_WR = "8";
        public const string AUX_RD = "9";
        public const string WR_STATUS_UPDATE_REQ = "2";
        public const string WR_STATUS_UPDATE_REQ_MOT = "6";
        public const string I2C_WR_MOT = "4";
        public const string I2C_RD_MOT = "5";
        public const string I2C_RD = "1";
        public const string I2C_WR = "0";
        public const string ACK = "0";

        public static decodedLine ParseLine(string previousLine, string line)
        {
            decodedLine values = new decodedLine();
            int previous_line_timestamp = 0, curr_line_timestamp = 0;
            string[] line_subs, prev_line_subs;
            string aux_data_string;

            // format: timestamp:aux_data
            // i.e: 
            // 078605583:90020600
            // 078605655:0011
            // if first char is not number, skip
            try
            {
                if (!char.IsNumber(line, 0))
                {
                    values.valid = false;
                    return values;
                }
            } catch(ArgumentOutOfRangeException)
            {
                // if blank/empty line
                values.valid = false;
                return values;
            }

            prev_line_subs = previousLine.Split(':');

            if (line.Contains(':'))
            {
                line_subs = line.Split(':');
                aux_data_string = line_subs[1];
            } else
            {
                values.valid = false;
                return values;
            }


            if (string.IsNullOrEmpty(previousLine))
            {
                values.duration = "0.0";
            }
            else
            {
                try
                {
                    previous_line_timestamp = int.Parse(prev_line_subs[0]);
                }
                catch (OverflowException e)
                {
                    Console.WriteLine(previousLine);
                    Console.WriteLine(e.Message);
                }

                try
                {
                    curr_line_timestamp = int.Parse(line_subs[0]);                    
                }
                catch (OverflowException e)
                {
                    Console.WriteLine(line);
                    Console.WriteLine(e.Message);
                }
                values.duration = (curr_line_timestamp - previous_line_timestamp).ToString();
            }

            // AUX header:
            // 1000: Native Write
            // 1001: Native Read
            // 0000: I2C Write
            // 0001: I2C Read
            // 0010: Write_Status_Update_Request
            // 0100: I2C Write(MOT)
            // 0101: I2C Read(MOT)
            // 0110: Write_Status_Update_Request(MOT)

            // Reply transaction: None (0000b must be padded to Command to form a byte) 
            //-----------------
            // __00 = AUX_ACK
            // 0000 = I2C ACK
            // 0100 = I2C NACK
            // 1000 = I2C DEFER
            //-----------------
            // 0000 = AUX ACK
            // 0001 = AUX NACK
            // 0010 = AUX DEFER
            // Basically only (0000 = I2C ACK) vs (0000: I2C Write) may confuse
            // all the other reply transaction shall be 1-byte lenght

            // 1-byte length: Native ACK/NACK/DEFER,  I2C ACK/NACK/DEFER are all 1-byte
            if (aux_data_string.Length == 2)
            {
                switch (aux_data_string)
                {
                    case "00":
                        values.decodedData = "ACK";
                        break;
                    case "10":
                        values.decodedData = "NACK";
                        break;
                    case "20":
                        values.decodedData = "DEFER";
                        break;
                    case "80":
                        values.decodedData = "I2C DEFER";
                        break;
                    case "40":
                        values.decodedData = "I2C NACK";
                        break;
                    default:
                        values.decodedData = "ERROR: unknown";
                        break;
                }
                values.valid = true;
                return values;
            }

        
        string command = aux_data_string.Substring(0, 1);
            switch(command)
            {
                case AUX_WR:
                    values.decodedData = "AUX Write";
                    break;
                case AUX_RD:
                    values.decodedData = "AUX Read";
                    break;
                case WR_STATUS_UPDATE_REQ:
                    values.decodedData = "Write_Status_Update_Request";
                    break;
                case WR_STATUS_UPDATE_REQ_MOT:
                    values.decodedData = "Write_Status_Update_Request(MOT)";
                    break;
                case I2C_WR_MOT:
                    values.decodedData = "I2C Write(MOT)";
                    break;
                case I2C_RD_MOT:
                    values.decodedData = "I2C Read(MOT)";
                    break;
                case I2C_RD:
                    values.decodedData = "I2C Read";
                    break;
                case I2C_WR: // (0000 = I2C ACK) vs (0000: I2C Write) may confuse
                    //values.decodedData = "I2C Write";
                    break;
            }

            values.valid = true;//debug
            return values;
        }

        public static bool IsI2CWrite(string line, string previousLine)
        {
            byte I2C7bitAddress = 0xFF; // make MSB!=1'b0 on purpose

            // Example #1
            //SYNC ► 0000|0000 ► 00000000 ► 0|1001000 ► STOP
            //(Address - only transaction with MOT = 0 and I2C address)
            // COMMAND | 0x00 | I2C address : Address-only request
            // at least 3-byte

            // Example #2
            //SYNC ► 0000|0000 ► 00000000 ► 0|1001000 ► 0000|0011 ► Data0 ► Data1 ► Data2 ► Data3 ► STOP
            //(MOT = 0, the same I2C address, Length = 4 bytes)

            // if 1st, 2nd byte is not 0x0000, it is not I2C Write request
            string I2CWRheader = "0000";
            if (!I2CWRheader.Equals(line.Substring(0, 4))) {
                return false;
            }
            try
            {
                I2C7bitAddress = Convert.ToByte(line.Substring(4, 2), 16);
            } 
            catch (ArgumentOutOfRangeException) // no 3rd byte(I2C address), return false
            {
                return false;
            }
            //pos 0 is least significant bit, pos 7 is most
            // bool IsBitSet(byte b, int pos)
            //{
            //    return (b & (1 << pos)) != 0;
            //}
            // MSB shall be 0 for I2C 7-bit slave address
            if ((I2C7bitAddress & (1 << 7)) != 0)
            {
                return false;
            }

            // if the last request is read
            // the next one shall NOT be I2C write
            string previousCommand = previousLine.Substring(0, 1);
            if (previousCommand.Equals(AUX_RD) ||
                previousCommand.Equals(I2C_RD_MOT) ||
                previousCommand.Equals(I2C_RD))
            {
                return false;
            }

            // check I2C write length meet its payload
            string lengthHexString = line.Substring(6, 2);
            int length = Int32.Parse(lengthHexString, System.Globalization.NumberStyles.HexNumber);
            length++;
            if (line.Length.Equals((length*2 + 8)))
            {
                return true;
            }

            return false;
        }
    }
}
