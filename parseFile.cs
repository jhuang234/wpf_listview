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
                }
                values.valid = true;
                return values;
            }



            values.valid = true;//debug
            return values;
        }
    }
}
