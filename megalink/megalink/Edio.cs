using System;
using System.IO;
using System.Text;
using System.IO.Ports;
using System.Threading;

namespace megalink
{
    public class FileInfo
    {
        const byte ATTR_DIR = 0b00010000; //32
        const byte ATTR_FILE = 0b00010000; //16
        public string name;
        public int size;
        public UInt16 date;
        public UInt16 time;
        public byte attrib;
        public bool IsDirectory()
        {
            return (attrib & ATTR_DIR) >> 4 == 1;
        }
        public override string ToString()
        {
            return $"{name.PadRight(30)} | {formatSize().PadLeft(7)} | date:{date} | time:{time} | attr: {(IsDirectory() ? "D" : "F")}";
        }

        string formatSize()
        {
            var fSize= size;
            if (fSize < 1024) return fSize + "b";
            fSize = fSize / 1024;
            if(fSize < 1024) return fSize + "kb";
            fSize = fSize / 1024;
            if(fSize < 1024) return fSize + "mb";
            fSize = fSize / 1024;
            return fSize + "gb";
        }
        
    }

    public class Vdc
    {
        public const int size = 8;
        public UInt16 v50;
        public UInt16 v25;
        public UInt16 v12;
        public UInt16 vbt;

        public Vdc(byte[] data)
        {
            v50 = BitConverter.ToUInt16(data, 0);
            v25 = BitConverter.ToUInt16(data, 2);
            v12 = BitConverter.ToUInt16(data, 4);
            vbt = BitConverter.ToUInt16(data, 6);
        }

    }

    public class Edio
    {

        const int ACK_BLOCK_SIZE = 1024;

        public const int MAX_ROM_SIZE = 0xF80000;

        public const int ADDR_ROM = 0x0000000;
        public const int ADDR_SRAM = 0x1000000;
        public const int ADDR_BRAM = 0x1080000;
        public const int ADDR_CFG = 0x1800000;
        public const int ADDR_SSR  = 0x1802000;
        public const int ADDR_FIFO = 0x1810000;

        public const int SIZE_ROMX = 0x1000000;
        public const int SIZE_SRAM = 0x80000;
        public const int SIZE_BRAM = 0x80000;

        public const int ADDR_FLA_MENU = 0x00000;       //boot fails m68K code
        public const int ADDR_FLA_FPGA = 0x40000;       //boot fails fpga code
        public const int ADDR_FLA_ICOR = 0x80000;       //mcu firmware update

        public const byte FAT_READ = 0x01;
        public const byte FAT_WRITE = 0x02;
        public const byte FAT_OPEN_EXISTING = 0x00;
        public const byte FAT_CREATE_NEW = 0x04;
        public const byte FAT_CREATE_ALWAYS = 0x08;
        public const byte FAT_OPEN_ALWAYS = 0x10;
        public const byte FAT_OPEN_APPEND = 0x30;

        public const byte HOST_RST_OFF    = 0;
        public const byte HOST_RST_SOFT   = 1;
        public const byte HOST_RST_HARD   = 2;

        const byte CMD_STATUS = 0x10;                   // checks last executed cmd status - success 0
        const byte CMD_GET_MODE = 0x11;                 // checks last executed cmd status - success 0
        const byte CMD_IO_RST = 0x12;                   // IO Reset/ Service Mode
        const byte CMD_GET_VDC = 0x13;                  // Checks VDC (Valtages ?)
        const byte CMD_RTC_GET = 0x14;                  // Get RTC Clock
        const byte CMD_RTC_SET = 0x15;                  // Set RTC Clock
        const byte CMD_FLA_RD = 0x16;                   // Reads Flash memory
        const byte CMD_FLA_WR = 0x17;                   // Writes Flash memory
        const byte CMD_FLA_WR_SDC = 0x18;               // Writes Flash memory (from SD card?) need path? 
        const byte CMD_MEM_RD = 0x19;                   // Reads from Memory (Mega drive?)
        const byte CMD_MEM_WR = 0x1A;                   // Writes block to Memory (Mega drive?)
        const byte CMD_MEM_SET = 0x1B;                  // Sets value in Memory (Mega drive?) 
        const byte CMD_MEM_TST = 0x1C;                  // Tests value in Memory (Mega drive?) 
        const byte CMD_MEM_CRC = 0x1D;                  // Check CRC in Memory (Mega drive?) 
        const byte CMD_FPG_USB = 0x1E;                  // Initializes the FPGA from file over usb
        const byte CMD_FPG_SDC = 0x1F;                  // Initializes the FPGA from file on sd card
        const byte CMD_FPG_FLA = 0x20;                  // Initializes the FPGA from address in flash
        const byte CMD_FPG_CFG = 0x21;
        const byte CMD_USB_WR = 0x22;
        const byte CMD_FIFO_WR = 0x23;
        const byte CMD_UART_WR = 0x24;
        const byte CMD_REINIT = 0x25;
        const byte CMD_SYS_INF = 0x26;                  // Get system info (like on everdrive ??)
        const byte CMD_GAME_CTR = 0x27;
        const byte CMD_UPD_EXEC = 0x28;                 // Execute update
        const byte CMD_HOST_RST = 0x29;                 // Reset Mega drive


        const byte CMD_DISK_INIT = 0xC0;                // Initialize SD card ??
        const byte CMD_DISK_RD = 0xC1;                  // Raw read SD card ??
        const byte CMD_DISK_WR = 0xC2;                  // Raw write SD card ??
        const byte CMD_F_DIR_OPN = 0xC3;            
        const byte CMD_F_DIR_RD = 0xC4;                 // Reads a directory to get info on current directory (name, etc)
        const byte CMD_F_DIR_LD = 0xC5;                 // Loads Directory before getting info (like bash cd)
        const byte CMD_F_DIR_SIZE = 0xC6;               // Directory Size (after load
        const byte CMD_F_DIR_PATH = 0xC7;               // Gets current path ?? I think
        const byte CMD_F_DIR_GET = 0xC8;                //Gets all files in directory
        //Files         
        const byte CMD_F_FOPN = 0xC9;                   // Opens file
        const byte CMD_F_FRD = 0xCA;                    // Reads contents of file
        const byte CMD_F_FRD_MEM = 0xCB;                // Reads file in memory / Loads file into memory ??
        const byte CMD_F_FWR = 0xCC;                    // Writes to opened file
        const byte CMD_F_FWR_MEM = 0xCD;                // Writes to opened file in memory??
        const byte CMD_F_FCLOSE = 0xCE;                 // Closes file
        const byte CMD_F_FPTR = 0xCF;                   // Sets poiinter to file address of opened file ??
        const byte CMD_F_FINFO = 0xD0;                  // get info of file by sd path
        const byte CMD_F_FCRC = 0xD1;                   // Gets CRC of opened file
        const byte CMD_F_DIR_MK = 0xD2;                 // Makes a new Directory
        const byte CMD_F_DEL = 0xD3;                    // Deletes File by ds path
            
        const byte CMD_USB_RECOV = 0xF0;                // Recovery mode
        const byte CMD_RUN_APP = 0xF1;                  // Return to app mode

        private SerialPort port;

        public Edio()
        {
            Logger.dbg("Edio()");

            seek();
        }

        public Edio(string port_name)
        {
            Logger.dbg("Edio(string port_name)");

            openConnection(port_name.Trim());
        }

        void seek()
        {
            Logger.dbg("seek()");

            string[] ports = SerialPort.GetPortNames();

            for (int i = 0; i < ports.Length; i++)
            {
                try
                {
                    openConnection(ports[i]);
                    Logger.inf($"Current Directory {Directory.GetCurrentDirectory()}");
                    return;
                }
                catch (Exception e)
                {
                    // Console.Out.WriteLine(e.Message + "\n"+ e.StackTrace);
                }
            }

            throw new Exception("EverDrive not found");
        }

        void openConnection(string pname)
        {
            Logger.dbg("openConnection(string pname)");
            try
            {
                port = new SerialPort(pname);
                port.ReadTimeout = 300;
                port.WriteTimeout = 300;
                port.Open();
                port.ReadExisting();
                getStatus();
                port.ReadTimeout = 2000;
                port.WriteTimeout = 2000;
                return;
            }
            catch (Exception e)
            {
                Logger.dbg($"Everdrive not found at {pname}");
            }
            
            try
            {
                port.Close();
            }
            catch (Exception e)
            {
                Logger.err($"Failed to close port {pname} :  {e.Message}");
            }
            finally
            {
                port = null;
                throw new Exception("EverDrive not found");
            }
            
        }
        public string PortName
        {
            get
            {
                return port.PortName;
            }
        }

        //************************************************************************************************ 

        void tx32(int arg)
        {
            Logger.dbg("tx32(int arg)");

            byte[] buff = new byte[4];
            buff[0] = (byte)(arg >> 24);
            buff[1] = (byte)(arg >> 16);
            buff[2] = (byte)(arg >> 8);
            buff[3] = (byte)(arg);

            txData(buff, 0, buff.Length);
        }

        int rx32()
        {
            Logger.dbg("rx32()");

            byte[] buff = new byte[4];
            rxData(buff, 0, buff.Length);
            return buff[3] | (buff[2] << 8) | (buff[1] << 16) | (buff[0] << 24);
        }


        void tx16(int arg)
        {
            Logger.dbg("tx16(int arg)");

            byte[] buff = new byte[2];
            buff[0] = (byte)(arg >> 8);
            buff[1] = (byte)(arg);

            txData(buff, 0, buff.Length);
        }

        public UInt16 rx16()
        {
            Logger.dbg("rx16()");

            byte[] buff = new byte[2];
            rxData(buff, 0, buff.Length);
            return (UInt16)(buff[1] | (buff[0] << 8));
        }

        void tx8(int arg)
        {
            Logger.dbg("tx8(int arg)");

            byte[] buff = new byte[1];
            buff[0] = (byte)(arg);
            txData(buff, 0, buff.Length);
        }

        public byte rx8()
        {
            Logger.dbg("rx8()");

            return (byte)port.ReadByte();
        }


        void txData(byte[] buff)
        {
            Logger.dbg("txData(byte[] buff)");

            txData(buff, 0, buff.Length);
        }

        void txData(byte[] buff, int offset, int len)
        {
            Logger.dbg("txData(byte[] buff, int offset, int len)");

            while (len > 0)
            {
                int block = 8192;
                if (block > len) block = len;

                port.Write(buff, offset, block);
                len -= block;
                offset += block;

            }
        }

        void txData(string str)
        {
            Logger.dbg("txData(string str)");

            port.Write(str);
        }



        void txDataACK(byte[] buff, int offset, int len)
        {
            Logger.dbg("txDataACK(byte[] buff, int offset, int len)");

            while (len > 0)
            {
                int resp = rx8();
                if (resp != 0) throw new Exception("tx ack: " + resp.ToString("X2"));

                int block = ACK_BLOCK_SIZE;
                if (block > len) block = len;

                txData(buff, offset, block);

                len -= block;
                offset += block;

            }
        }


        void rxData(byte[] buff, int offset, int len)
        {
            Logger.dbg("rxData(byte[] buff, int offset, int len)");

            for (int i = 0; i < len;)
            {
                i += port.Read(buff, offset + i, len - i);

            }
        }

        public byte[] rxData(int len)
        {
            Logger.dbg("rxData(int len)");

            byte[] buff = new byte[len];
            rxData(buff, 0, len);
            return buff;
        }

        void rxData(byte[] buff, int len)
        {
            Logger.dbg("rxData(byte[] buff, int len)");

            rxData(buff, 0, len);
        }

        void txString(string str)
        {
            Logger.dbg("txString(string str)");

            tx16(str.Length);
            txData(str);
        }

        string rxString()
        {
            Logger.dbg("rxString()");

            int len = rx16();
            byte[] buff = new byte[len];
            rxData(buff, 0, buff.Length);
            return System.Text.Encoding.UTF8.GetString(buff);
        }

        FileInfo rxFileInfo()
        {
            Logger.dbg("rxFileInfo()");

            FileInfo inf = new FileInfo();

            inf.size = rx32();
            inf.date = rx16();
            inf.time = rx16();
            inf.attrib = rx8();
            inf.name = rxString();

            return inf;
        }

        public SerialPort getPort()
        {
            Logger.dbg("getPort()");

            return port;
        }

        public int dataAvailable()
        {
            Logger.dbg("dataAvailable()");

            return port.BytesToRead;
        }

        public void flush()
        {
            Logger.dbg("checkStatus()");

            int len = dataAvailable();
            if (len > 0x10000) len = 0x10000;
            byte[] buff = new byte[len];
            port.Read(buff, 0, buff.Length);
        }

        //************************************************************************************************ 

        void txCMD(byte cmd_code)
        {
            Logger.dbg("checkStatus()");

            byte[] cmd = new byte[4];
            cmd[0] = (byte)('+');
            cmd[1] = (byte)('+' ^ 0xff);
            cmd[2] = cmd_code;
            cmd[3] = (byte)(cmd_code ^ 0xff);
            txData(cmd);
        }

        void checkStatus()
        {
            Logger.dbg("checkStatus()");

            int resp = getStatus();
            if (resp != 0) throw new Exception("operation error: " + resp.ToString("X2"));
        }

        public int getStatus()
        {
            Logger.dbg("dirPath()");

            int resp;
            txCMD(CMD_STATUS);
            resp = rx16();
            if ((resp & 0xff00) != 0xA500) throw new Exception("unexpected status response (" + resp.ToString("X4") + ")");
            return resp & 0xff;
        }


        public void diskInit()
        {
            Logger.dbg("diskInit()");

            txCMD(CMD_DISK_INIT);
            checkStatus();
        }

        public void diskRead(int addr, byte slen, byte[] buff)
        {
            Logger.dbg("diskRead(int addr, byte slen, byte[] buff)");

            byte resp;

            txCMD(CMD_DISK_RD);
            tx32(addr);
            tx32(slen);


            for (int i = 0; i < slen; i++)
            {
                resp = (byte)port.ReadByte();
                if (resp != 0) throw new Exception("disk read error: " + resp);
                rxData(buff, i * 512, 512);
            }

        }
        


        public void dirOpen(string path)
        {
            Logger.dbg("dirPath()");

            txCMD(CMD_F_DIR_OPN);
            txString(path);
            try
            {
                checkStatus();
            }
            catch (Exception e)
            {
                Logger.dbg($"Could not open \"{path}\": {e}");
                throw;
            }
        }

        public void dirPath()
        {
            Logger.dbg("dirPath()");

            txCMD(CMD_F_DIR_PATH);
            rxString();
            try
            {
                checkStatus();
            }
            catch (Exception e)
            {
                // Console.Out.WriteLine($"Could not open \"{path}\": {e}");
                throw;
            }
        }

        public FileInfo dirRead(UInt16 max_name_len)
        {
            Logger.dbg("dirRead(UInt16 max_name_len)");

            int resp;
            if (max_name_len == 0) max_name_len = 0xffff;
            txCMD(CMD_F_DIR_RD);
            tx16(max_name_len);//max name lenght
            resp = rx8();

            if (resp != 0) throw new Exception($"dir read error: {resp:X2}");

            return rxFileInfo();

        }

        public void dirLoad(string path, int sorted)
        {
            Logger.dbg("dirLoad(string path, int sorted)");

            txCMD(CMD_F_DIR_LD);
            tx8(sorted);
            txString(path);
            checkStatus();
        }


        public int dirGetSize()
        {
            txCMD(CMD_F_DIR_SIZE);
            return rx16();
        }

        public FileInfo[] dirGetRecs(int start_idx, int amount, int max_name_len)
        {
            Logger.dbg("dirGetRecs(int start_idx, int amount, int max_name_len)");

            FileInfo[] inf = new FileInfo[amount];
            byte resp;

            txCMD(CMD_F_DIR_GET);
            tx16(start_idx);
            tx16(amount);
            tx16(max_name_len);

            
            for (int i = 0; i < amount; i++)
            {
                resp = rx8();
                if (resp != 0) throw new Exception($"dir read error: {resp:X2}");
                inf[i] = rxFileInfo();

            }

            return inf;
        }

        public void dirMake(string path)
        {
            Logger.dbg("dirMake(string path)");

            txCMD(CMD_F_DIR_MK);
            txString(path);
            int resp = getStatus();
            if (resp != 0 && resp != 8)//ignore error 8 (already exist)
            {
                checkStatus();
            }
        }

        public void fileOpen(string path, int mode)
        {
            Logger.dbg("fileOpen(string path, int mode)");

            txCMD(CMD_F_FOPN);
            tx8(mode);
            txString(path);
            checkStatus();
        }

        public void fileRead(byte[] buff, int offset, int len)
        {
            Logger.dbg("fileRead(int addr, int len)");


            txCMD(CMD_F_FRD);
            tx32(len);


            while (len > 0)
            {
                int block = 4096;
                if (block > len) block = len;
                int resp = rx8();
                if (resp != 0) throw new Exception($"file read error: {resp:X2}");

                rxData(buff, offset, block);
                offset += block;
                len -= block;

            }

        }

        public void fileRead(int addr, int len)
        {
            Logger.dbg("fileRead(int addr, int len)");


            while (len > 0)
            {
                int block = 0x10000;
                if (block > len) block = len;

                txCMD(CMD_F_FRD_MEM);
                tx32(addr);
                tx32(block);
                tx8(0);//exec
                checkStatus();

                len -= block;
                addr += block;

            }

        }

        public void fileWrite(byte[] buff, int offset, int len)
        {
            Logger.dbg("fileWrite(byte[] buff, int offset, int len)");

            txCMD(CMD_F_FWR);
            tx32(len);
            txDataACK(buff, offset, len);
            checkStatus();
        }

        public void fileWrite(int addr, int len)
        {
            Logger.dbg("fileWrite(int addr, int len)");
            
            while (len > 0)
            {
                int block = 0x10000;
                if (block > len) block = len;

                txCMD(CMD_F_FWR_MEM);
                tx32(addr);
                tx32(block);
                tx8(0);//exec
                checkStatus();

                len -= block;
                addr += block;

            }
        }

        public void fileSetPtr(int addr)
        {
            Logger.dbg("fileSetPtr(int addr)");
            txCMD(CMD_F_FPTR);
            tx32(addr);
            checkStatus();
        }

        public void fileClose()
        {
            Logger.dbg("fileClose()");
            txCMD(CMD_F_FCLOSE);
            checkStatus();
        }

        public void delRecord(string path)
        {
            Logger.dbg("delRecord(string path)");
            txCMD(CMD_F_DEL);
            txString(path);
            checkStatus();
        }


        public void memWR(int addr, byte[] buff, int offset, int len)
        {
            Logger.dbg("memWR(int addr, byte[] buff, int offset, int len)");
            if (len == 0) return;
            txCMD(CMD_MEM_WR);
            tx32(addr);
            tx32(len);
            tx8(0);//exec
            txData(buff, offset, len);
        }

        public void memRD(int addr, byte[] buff, int offset, int len)
        {
            Logger.dbg("memRD(int addr, byte[] buff, int offset, int len)");
            if (len == 0) return;
            txCMD(CMD_MEM_RD);
            tx32(addr);
            tx32(len);
            tx8(0);//exec
            rxData(buff, offset, len);
        }

        public FileInfo fileInfo(string path)
        {
            Logger.dbg("fileInfo(string path)");
            txCMD(CMD_F_FINFO);
            txString(path);
            int resp = rx8();
            if (resp != 0) throw new Exception($"file access error: {resp:X2}");
            return rxFileInfo();

        }

        public void fifoWR(byte[] data, int offset, int len)
        {
            memWR(ADDR_FIFO, data, offset, len);
        }

        public void fifoWR(string str)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(str);
            memWR(ADDR_FIFO, bytes, 0, bytes.Length);
        }

        public void fifoTxString(string str)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(str);
            byte[] len = new byte[2];
            len[0] = (byte)(bytes.Length >> 8);
            len[1] = (byte)(bytes.Length & 0xff);
            fifoWR(len, 0, 2);
            fifoWR(bytes, 0, bytes.Length);
        }

        public void fifoTX32(int arg)
        {
            byte[] buff = new byte[4];
            buff[0] = (byte)(arg >> 24);
            buff[1] = (byte)(arg >> 16);
            buff[2] = (byte)(arg >> 8);
            buff[3] = (byte)(arg);

            fifoWR(buff, 0, buff.Length);
        }

        public void memSet(byte val, int addr, int len)
        {
            Logger.dbg("memSet(byte, int, int)");
            txCMD(CMD_MEM_SET);
            tx32(addr);
            tx32(len);
            tx8(val);
            tx8(0);//exec
            checkStatus();
        }

        public bool memTest(byte val, int addr, int len)
        {
            Logger.dbg("memTest(byte, int, int)");
            txCMD(CMD_MEM_TST);
            tx32(addr);
            tx32(len);
            tx8(val);
            tx8(0);//exec

            if (rx8() == 0) return false;

            return true;
        }


        public UInt32 memCRC(int addr, int len)
        {
            Logger.dbg("memCRC(int, int)");
            txCMD(CMD_MEM_CRC);
            tx32(addr);
            tx32(len);
            tx32(0);//crc init val
            tx8(0);//exec

            return (UInt32)rx32();
        }

        public UInt32 fileCRC(int len)
        {
            Logger.dbg("fileCRC(int)");
            int resp;
            txCMD(CMD_F_FCRC);
            tx32(len);
            tx32(0);//crc init val

            resp = rx8();
            if (resp != 0) throw new Exception("Disk read error: " + resp.ToString("X2"));


            return (UInt32)rx32();
        }
        
        //Reads Flash memory
        public void flaRD(int addr, byte[] buff, int offset, int len)
        {
            Logger.dbg("flaRD(int, byte[], int, int)");
            txCMD(CMD_FLA_RD);
            tx32(addr);
            tx32(len);
            rxData(buff, offset, len);
        }


        //Writes to Flash memory
        public void flaWR(int addr, byte[] buff, int offset, int len)
        {
            Logger.dbg("flaWR(int, byte[], int, int)");
            txCMD(CMD_FLA_WR);
            tx32(addr);
            tx32(len);
            txDataACK(buff, offset, len);
            checkStatus();
        }

        public void fpgInit(byte[] data)
        {
            Logger.dbg("fpgInit(byte[])");
            txCMD(CMD_FPG_USB);
            tx32(data.Length);
            txDataACK(data, 0, data.Length);
            checkStatus();
        }


        public void fpgInit(int flash_addr)
        {
            Logger.dbg("fpgInit(int)");
            txCMD(CMD_FPG_FLA);
            tx32(flash_addr);
            tx8(0);//exec
            checkStatus();
        }

        public void fpgInit(string sd_path)
        {
            Logger.dbg("fpgInit(string)");
            FileInfo f = fileInfo(sd_path);
            fileOpen(sd_path, FAT_READ);
            txCMD(CMD_FPG_SDC);
            tx32(f.size);
            tx8(0);
            checkStatus();
        }


        
        public bool isServiceMode()
        {
            Logger.dbg("isServiceMode");
            txCMD(CMD_GET_MODE);
            byte resp = rx8();
            if (resp == 0xA1) return true;
            return false;
        }

        public Vdc GetVdc()
        {
            Logger.dbg("GetVdc");
            txCMD(CMD_GET_VDC);
            byte[] buff = rxData(Vdc.size);
            Vdc vdc = new Vdc(buff);
            return vdc;
        }

        public RtcTime rtcGet()
        {
            Logger.dbg("RTC Get");
            txCMD(CMD_RTC_GET);
            byte[] buff = rxData(RtcTime.size);
            RtcTime rtc = new RtcTime(buff);
            return rtc;
        }

        public void rtcSet(DateTime dt)
        {
            Logger.dbg("RTC Set");
            RtcTime rtc = new RtcTime(dt);
            byte[] vals = rtc.getVals();
            txCMD(CMD_RTC_SET);
            txData(vals);
        }

        public void hostReset(byte rst)
        {
            Logger.dbg("Host Reset");
            txCMD(CMD_HOST_RST);
            tx8(rst);
        }
        //************************************************************************************************ usb service mode. System enters in service mode if cart powered via usb only
        public void recovery()
        {
            if (!isServiceMode())
            {
                throw new Exception("Device not in service mode");
            }


            byte[] crc = new byte[4];
            flaRD(ADDR_FLA_ICOR + 4, crc, 0, 4);
            int crc_int = (crc[0] << 0) | (crc[1] << 8) | (crc[2] << 16) | (crc[3] << 24);


            int old_tout = port.ReadTimeout;
            port.ReadTimeout = 8000;

            txCMD(CMD_USB_RECOV);
            tx32(ADDR_FLA_ICOR);
            tx32(crc_int);
            //txData(crc);
            int status = getStatus();

            port.ReadTimeout = old_tout;

            if (status == 0x88)
            {
                throw new Exception("current core matches to recovery copy");
            }

            if (status != 0)
            {
                throw new Exception($"recovery error: {status:X2}");
            }

        }

        public void exitServiceMode()
        {

            if (!isServiceMode()) return;

            txCMD(CMD_RUN_APP);
            bootWait();
            if (isServiceMode())
            {
                throw new Exception("Device stuck in service mode");
            }
        }

        public void enterServiceMode()
        {
            if (isServiceMode()) return;

            txCMD(CMD_IO_RST);
            tx8(0);
            bootWait();

            if (!isServiceMode())
            {
                throw new Exception("device stuck in APP mode");
            }
        }

        void bootWait()
        {

            for (int i = 0; i < 10; i++)
            {
                try
                {
                    Thread.Sleep(100);
                    port.Close();
                    Thread.Sleep(100);
                    port.Open();
                    getStatus();
                    return;
                }
                catch (Exception) { }
            }

            throw new Exception("boot timeout");
        }
    }



}
