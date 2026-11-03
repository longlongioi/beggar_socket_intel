using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Rebar;

namespace ChisFlashBurner
{
    public partial class Form1
    {
        /**
         * |             | ROM                             | RAM                      |                                                                                                                                                             |
         * | 菜单        | 0 0000 BBBB BBxx xxxx xxxx xxxx | 0000 0BBx xxxx xxxx xxxx | 1m 32k                                                                                                                                                      |
         * | 游戏1       | 0 0001 BBBB BBxx xxxx xxxx xxxx | 0000 0BBx xxxx xxxx xxxx | 1m 32k                                                                                                                                                      |
         * | 游戏2       | 0 001B BBBB BBxx xxxx xxxx xxxx | 0000 1BBx xxxx xxxx xxxx | 2m 32k                                                                                                                                                      |
         * | 游戏3       | 0 010B BBBB BBxx xxxx xxxx xxxx | 0001 0BBx xxxx xxxx xxxx | 2m 32k                                                                                                                                                      |
         * | 游戏4       | 0 011B BBBB BBxx xxxx xxxx xxxx | 0001 1BBx xxxx xxxx xxxx | 2m 32k                                                                                                                                                      |
         * | max         |                                 |                          |                                                                                                                                                             |
         * | 游戏5       | 0 100B BBBB BBxx xxxx xxxx xxxx | 0010 0BBx xxxx xxxx xxxx | 2m 32k                                                                                                                                                      |
         * | 游戏6       | 0 101B BBBB BBxx xxxx xxxx xxxx | 0010 1BBx xxxx xxxx xxxx | 2m 32k                                                                                                                                                      |
         * | 游戏7       | 0 110B BBBB BBxx xxxx xxxx xxxx | 0011 0BBx xxxx xxxx xxxx | 2m 32k                                                                                                                                                      |
         * | 游戏8       | 0 111B BBBB BBxx xxxx xxxx xxxx | 0011 1BBx xxxx xxxx xxxx | 2m 32k                                                                                                                                                      |
         * | 游戏9       | 1 000B BBBB BBxx xxxx xxxx xxxx | 0100 0BBx xxxx xxxx xxxx | 2m 32k                                                                                                                                                      |
         * | 游戏10      | 1 001B BBBB BBxx xxxx xxxx xxxx | 0100 1BBx xxxx xxxx xxxx | 2m 32k                                                                                                                                                      |
         * | 游戏11      | 1 010B BBBB BBxx xxxx xxxx xxxx | 0101 0BBx xxxx xxxx xxxx | 2m 32k                                                                                                                                                      |
         * | 游戏12      | 1 011B BBBB BBxx xxxx xxxx xxxx | 0101 1BBx xxxx xxxx xxxx | 2m 32k                                                                                                                                                      |
         * | 游戏13      | 1 100B BBBB BBxx xxxx xxxx xxxx | 0110 0BBx xxxx xxxx xxxx | 2m 32k                                                                                                                                                      |
         * | 游戏14      | 1 101B BBBB BBxx xxxx xxxx xxxx | 0110 1BBx xxxx xxxx xxxx | 2m 32k                                                                                                                                                      |
         * | 游戏15      | 1 110B BBBB BBxx xxxx xxxx xxxx | 0111 0BBx xxxx xxxx xxxx | 2m 32k                                                                                                                                                      |
         * | 游戏16      | 1 111B BBBB BBxx xxxx xxxx xxxx | 0111 1BBx xxxx xxxx xxxx | 2m 32k  
        */
        UInt32[,] mbc5_multiCardAddrRange_rom = new UInt32[,]
        {
            //   from         to
            { 0, 0 },                   // 整卡
            { 0x00000000, 0x000fffff }, // 菜单
            { 0x00100000, 0x001fffff }, // 游戏1
            { 0x00200000, 0x003fffff }, // 游戏2
            { 0x00400000, 0x005fffff }, // 游戏3
            { 0x00600000, 0x007fffff }, // 游戏4
            { 0x00800000, 0x009fffff }, // 游戏5
            { 0x00a00000, 0x00bfffff }, // 游戏6
            { 0x00c00000, 0x00dfffff }, // 游戏7
            { 0x00e00000, 0x00ffffff }, // 游戏8
            { 0x01000000, 0x011fffff }, // 游戏9
            { 0x01200000, 0x013fffff }, // 游戏10
            { 0x01400000, 0x015fffff }, // 游戏11
            { 0x01600000, 0x017fffff }, // 游戏12
            { 0x01800000, 0x019fffff }, // 游戏13
            { 0x01a00000, 0x01bfffff }, // 游戏14
            { 0x01c00000, 0x01dfffff }, // 游戏15
            { 0x01e00000, 0x01ffffff }  // 游戏16
        };

        UInt32[,] mbc5_multiCardAddrRange_ram = new UInt32[,]
        {
            { 0,0 },             // 整卡
            { 0x00000, 0x07fff },// 菜单
            { 0x00000, 0x07fff}, // 游戏1
            { 0x08000, 0x0ffff}, // 游戏2
            { 0x10000, 0x17fff}, // 游戏3
            { 0x18000, 0x1ffff}, // 游戏4
            { 0x20000, 0x27fff}, // 游戏5
            { 0x28000, 0x2ffff}, // 游戏6
            { 0x30000, 0x37fff}, // 游戏7
            { 0x38000, 0x3ffff}, // 游戏8
            { 0x40000, 0x47fff}, // 游戏9
            { 0x48000, 0x4ffff}, // 游戏10
            { 0x50000, 0x57fff}, // 游戏11
            { 0x58000, 0x5ffff}, // 游戏12
            { 0x60000, 0x67fff}, // 游戏13
            { 0x68000, 0x6ffff}, // 游戏14
            { 0x70000, 0x77fff}, // 游戏15
            { 0x78000, 0x7ffff}  // 游戏16
        };

        int mbc_BaseAddressOfBank(int mbcType, int bank)
        {
            switch (mbcType)
            {
                //case 1:
                //    return 0x4000;
                //case 2:
                //    return 0x4000;
                case 3:
                    if (bank == 0)
                        return 0x0000;
                    else
                        return 0x4000;
                case 5:
                default:
                    return 0x4000;
            }
        }

        void mbc_romSwitchBank(int mbcType, int bank)
        {
            if (bank < 0)
                return;

            byte b0, b1;

            switch (mbcType)
            {
                //case 1:
                //    b0 = (byte)(bank & 0x1f);
                //    b1 = (byte)(bank>>5);
                //    gbcCart_write(0x6000, new byte[] { 1 }); // Banking Mode Select advanced
                //    gbcCart_write(0x2000, new byte[] { b0 }); // 0x01 - 0x1f
                //    gbcCart_write(0x2000, new byte[] { b1 }); // 0x01 - 0x03
                //    break;
                //case 2:
                //    b0 = (byte)(bank & 0x0f);
                //    gbcCart_write(0x2100, new byte[] { b0 }); // 0x00 - 0x0f
                //    break;
                case 3:
                    b0 = (byte)((bank) & 0xff);
                    b0 = (b0 == 0) ? (byte)1 : b0;
                    gbcCart_write(0x2000, new byte[] { b0 }); // 0-255
                    break;
                case 5:
                default:
                    b0 = (byte)((bank) & 0xff);
                    b1 = (byte)((bank >> 8) & 0xff);
                    gbcCart_write(0x3000, new byte[] { b1 });
                    gbcCart_write(0x2000, new byte[] { b0 }); // 0-511
                    break;
            }
        }

        void mbc_ramSwitchBank(int mbcType, int bank)
        {
            if (bank < 0)
                return;

            byte b;

            switch (mbcType)
            {
                //case 1:
                //    b = (byte)((bank) & 0x03);
                //    gbcCart_write(0x6000, new byte[] { 0 }); // Banking Mode Select simple
                //    gbcCart_write(0x4000, new byte[] { b });
                //    break;
                //case 2:
                //    break;
                case 3:
                    b = (byte)((bank) & 0x07);
                    gbcCart_write(0x4000, new byte[] { b });
                    break;
                case 5:
                default:
                    b = (byte)((bank) & 0xff);
                    gbcCart_write(0x4000, new byte[] { b });
                    break;
            }
        }

        byte[] mbc5_romGetID()
        {

            byte[] id0 = new byte[1];
            byte[] id1 = new byte[1];
            byte[] id2 = new byte[1];
            byte[] id3 = new byte[1];

            // enter autoselect
            gbcCart_write(0xaaa, new byte[] { 0x90 });
            // read id
            gbcCart_read(0x00, ref id0);  //厂商代码 1C
            gbcCart_read(0x02, ref id1);  //设备代码 20
            gbcCart_read(0x1c, ref id2);  //
            gbcCart_read(0x1e, ref id3);  //
            // reset
            gbcCart_write(0x00, new byte[] { 0x50 });
			gbcCart_write(0x00, new byte[] { 0xff });
            byte[] id = { id0[0], id1[0], id2[0], id3[0] };
            return id;
        }


        void mbc5_romGetSize(out int bufferWriteBytes, out int deviceSize)
        {
            byte[] cfi = new byte[1];
            int temp;

            // CFI Query
            gbcCart_write(0xaa, new byte[] { 0x98 });

            gbcCart_read(0x4e, ref cfi);
            deviceSize = (int)Math.Pow(2, cfi[0]); // 容量大小

            gbcCart_read(0x56, ref cfi);
            temp = cfi[0] << 8;
            gbcCart_read(0x54, ref cfi);
            temp |= cfi[0];
            if (temp == 0)
                bufferWriteBytes = 0;
            else
                bufferWriteBytes = (int)Math.Pow(2, cfi[0]); //最大写入buff

            // read array
			gbcCart_write(0x00, new byte[] { 0x50 });
            gbcCart_write(0x00, new byte[] { 0xff });
        }


        int[] mbc5_sectorInfo()
        {
            byte[] cfi0 = new byte[1];
            byte[] cfi1 = new byte[1];
            int blockRegionCount;
            int sectorCount0, sectorSize0;
            int sectorCount1, sectorSize1;
            int bootFlag;

            // CFI Query
            gbcCart_write(0xaa, new byte[] { 0x98 });

            gbcCart_read(0x58, ref cfi0);
            blockRegionCount = cfi0[0];

            gbcCart_read(0x5a, ref cfi0);
            gbcCart_read(0x5c, ref cfi1);
            sectorCount0 = ((cfi1[0] << 8) | cfi0[0]) + 1;

            gbcCart_read(0x5e, ref cfi0);
            gbcCart_read(0x60, ref cfi1);
            sectorSize0 = ((cfi1[0] << 8) | cfi0[0]) * 256;

            gbcCart_read(0x62, ref cfi0);
            gbcCart_read(0x64, ref cfi1);
            sectorCount1 = ((cfi1[0] << 8) | cfi0[0]) + 1;

            gbcCart_read(0x66, ref cfi0);
            gbcCart_read(0x68, ref cfi1);
            sectorSize1 = ((cfi1[0] << 8) | cfi0[0]) * 256;

            gbcCart_read(0x9e, ref cfi0);
            bootFlag = cfi0[0];

            // readarray
			gbcCart_write(0x00, new byte[] { 0x50 });
            gbcCart_write(0x00, new byte[] { 0xff });

            int[] sectorInfo;
            if (blockRegionCount == 1)
                sectorInfo = new int[sectorCount0];
            else
                sectorInfo = new int[sectorCount0 + sectorCount1];

            if (sectorInfo.Length > 65535)
                return sectorInfo;

            // 02h bottom boot
            if ((bootFlag == 2 || bootFlag == 12) && blockRegionCount == 2)
            {
                for (int i = 0; i < sectorCount0; i++)
                    sectorInfo[i] = i * sectorSize0;
                for (int i = 0; i < sectorCount1; i++)
                    sectorInfo[sectorCount0 + i] = sectorCount0 * sectorSize0 + i * sectorSize1;
            }
            // 03h top boot
            else if (bootFlag == 3 && blockRegionCount == 2)
            {
                for (int i = 0; i < sectorCount1; i++)
                    sectorInfo[i] = i * sectorSize1;
                for (int i = 0; i < sectorCount0; i++)
                    sectorInfo[sectorCount1 + i] = sectorCount1 * sectorSize1 + i * sectorSize0;
            }
            // 04h uniform sectors
            else
            {
                for (int i = 0; i < sectorCount0; i++)
                    sectorInfo[i] = i * sectorSize0;
            }

            return sectorInfo;
        }
		
		
        void mbc5_romEraseSector(int addrFrom, int addrTo)
        {
            int mbcType = mbcTypeSelected();
            int[] sectorInfo = mbc5_sectorInfo();

            printLog(string.Format("擦除 0x{0:x8} - 0x{1:x8}", addrFrom, addrTo));

            // 获取区间
            int sectorStart = -1;
            int sectorEnd = -1;

            for (int i = 0; i < sectorInfo.Length; i++)
            {
                if (sectorInfo[i] <= addrFrom)
                    sectorStart = i;
                if (sectorInfo[i] >= addrTo)
                    break;
                else
                    sectorEnd = i;
            }

            // 开始擦
            int sectorCount = sectorEnd - sectorStart + 1;

            for (int i = sectorCount; i > 0; i--)
            {
                int sa = sectorInfo[sectorStart + i - 1];

                printLog(string.Format("    0x{0:x8}", sa));
                showProgress(i, sectorCount);

                int bank = sa >> 14;
                mbc_romSwitchBank(mbcType, bank);
				Thread.Sleep(20);
				
                int _sa = mbc_BaseAddressOfBank(mbcType, bank);
                _sa += (sa & 0x3fff);
				
				gbcCart_write((UInt32)_sa, new byte[] { 0x50 });
                gbcCart_write((UInt32)_sa, new byte[] { 0x20 });
                gbcCart_write((UInt32)_sa, new byte[] { 0xD0 });   // Sector Erase

                byte[] temp = new byte[1];
                do
                {
                    gbcCart_read((UInt32)_sa, ref temp);
                    Thread.Sleep(20);
                } while (temp[0] != 0xff);
            }
        }

        int mbc5_romCalEraseTime()
        {
            // 获取rom flash buffer大小
            int deviceSize, bufferWriteBytes;
            mbc5_romGetSize(out bufferWriteBytes, out deviceSize);

            int[] secotrInfo = mbc5_sectorInfo();
            int sectorCount = secotrInfo.Length;

            byte[] cfi = new byte[1];

            // CFI Query
            gbcCart_write(0xaa, new byte[] { 0x98 });

            //Typical timeout for full chip erase = 2^n ms
            gbcCart_read(0x42, ref cfi);
            int timeout_block = (int)Math.Pow(2, cfi[0]);

            //Typical timeout for full chip erase = 2^n ms
            gbcCart_read(0x44, ref cfi);
            int timeout_chip = (int)Math.Pow(2, cfi[0]);

            ////Maximum timeout for chip erase = 2^n times typical
            //gbcCart_read(0x4c, ref cfi);
            //int timeout_chipMax = (int)Math.Pow(2, cfi[0]);

            // reset
            gbcCart_write(0x00, new byte[] { 0x50 });
			gbcCart_write(0x00, new byte[] { 0xff });

            //return timeout_chip * timeout_chipMax;
            //return (int)(timeout_chip * 1.5);
            //return timeout_chip;
            if (timeout_chip == 1)
                return timeout_block * sectorCount;
            else
                return timeout_chip;
        }

        ////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////

        void mission_readRomID_mbc5()
        {
            // 切换电源
            if (checkBox_mbc5V.Checked)
            {
                printLog("输出5V");
                pwr_5v_enabled = true;
                cart_power(false, false); // power down
                Thread.Sleep(111);
                cart_power(true, true); // 5v out
                Thread.Sleep(333);
            }

            byte[] id = mbc5_romGetID();

            printLog(BitConverter.ToString(id).Replace("-", " "));

            if (id.Take(2).SequenceEqual(new byte[] { 0xc2, 0xcb }))
                printLog("MX29LV640EB");
            else if (id.Take(2).SequenceEqual(new byte[] { 0xc2, 0xc9 }))
                printLog("MX29LV640ET");
            else if (id.Take(2).SequenceEqual(new byte[] { 0x1c, 0x20 }))
                printLog("M5M29GT320VP-80");			
            else if (id.SequenceEqual(new byte[] { 0x01, 0x7e, 0x22, 0x01 }))
                printLog("S29GL256N");
            else if (id.SequenceEqual(new byte[] { 0x89, 0x7e, 0x22, 0x01 }))
                printLog("JS28F256");
            else if (id.Take(3).SequenceEqual(new byte[] { 0x01, 0x7e, 0x10 }))
                printLog("W29GL064/S29GL064");
            else
                printLog("ID暂未收录，可能无法写入");

            int deviceSize, bufferWriteBytes;
            mbc5_romGetSize(out bufferWriteBytes, out deviceSize);

            int[] sectorInfo = mbc5_sectorInfo();

            printLog(string.Format("容量:{0:d} 扇区数量:{1:d} 扇区大小:{2:d} BuffWr:{3:d}",
                                    deviceSize,
                                    sectorInfo.Length,
                                    sectorInfo[1],
                                    bufferWriteBytes)
            );

            if (pwr_5v_enabled)
            {
                pwr_5v_enabled = false;
                cart_power(true, false); // 3.3v out
                printLog("关闭5V");
            }
            port.Close();
            enableButton();
        }

        void mission_eraseChip_mbc5()
        {

            // 切换电源
            if (checkBox_mbc5V.Checked)
            {
                printLog("输出5V");
                pwr_5v_enabled = true;
                cart_power(false, false); // power down
                Thread.Sleep(111);
                cart_power(true, true); // 5v out
                Thread.Sleep(333);
            }

            // 获取擦除时间
            int eraseTimeMs = mbc5_romCalEraseTime();
			byte[] temp = new byte[1];
			
			gbcCart_read(0, ref temp);			
            gbcCart_write(0x1FFFFF, new byte[] { 0xa7 });
            gbcCart_write(0x1FFFFF, new byte[] { 0xd0 }); // Chip Erase

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();


            do
            {
                long t = stopwatch.ElapsedMilliseconds;
                if (t > eraseTimeMs * 0.9)
                    t = (long)(eraseTimeMs * 0.9);
                showProgress((int)t, eraseTimeMs);

                Thread.Sleep(1000);
				gbcCart_write(0x000, new byte[] { 0xff });
                gbcCart_read(0, ref temp);
                printLog(string.Format("..... {0:x2}", temp[0]));
            } while (temp[0] != 0xff);

            stopwatch.Stop();
            showProgress(eraseTimeMs, eraseTimeMs);
            printLog("擦除完毕");
            printLog(string.Format("擦除耗时 {0:f3} s", stopwatch.ElapsedMilliseconds / 1000.0f));

            if (pwr_5v_enabled)
            {
                pwr_5v_enabled = false;
                cart_power(true, false); // 3.3v out
                printLog("关闭5V");
            }
            port.Close();
            enableButton();
        }

        void mission_programRom_mbc5()
        {
            // 打开文件
            string romFilePath = textBox_romPath.Text;

            //FileStream file;
            try
            {
                file = new FileStream(romFilePath, FileMode.Open, FileAccess.Read);
            }
            catch (System.IO.IOException)
            {
                printLog("文件被占用");
                port.Close();
                enableButton();
                return;
            }

            int fileLength = (int)file.Length;

            byte[] rom = new byte[fileLength];
            file.Read(rom, 0, fileLength);
            file.Close();

            // 切换电源
            if (checkBox_mbc5V.Checked)
            {
                printLog("输出5V");
                pwr_5v_enabled = true;
                cart_power(false, false); // power down
                Thread.Sleep(111);
                cart_power(true, true); // 5v out
                Thread.Sleep(333);
            }

            // 获取rom flash buffer大小
            int deviceSize, bufferWriteBytes;
            mbc5_romGetSize(out bufferWriteBytes, out deviceSize);

            // 获取id
            byte[] id = mbc5_romGetID();
            if (id.Take(2).SequenceEqual(new byte[] { 0x1c, 0x20 }) || id.SequenceEqual(new byte[] { 0x89, 0x7e, 0x22, 0x01 })) // js28
                bufferWriteBytes = 256;                                  // For a x8 device, maximum buffer size is 256 bytes;

            // 获取工作区间
            int addrBegin, addrEnd;
            int gameSelect = comboBox_mbc5MultiCartSelect.SelectedIndex;
            if (gameSelect != 0)
            {
                addrBegin = (int)mbc5_multiCardAddrRange_rom[gameSelect, 0];
                addrEnd = (int)mbc5_multiCardAddrRange_rom[gameSelect, 1];
            }
            else
            {
                addrBegin = 0;
                addrEnd = deviceSize - 1;
            }

            // 检查rom容量
            if (fileLength > (addrEnd - addrBegin + 1) || (addrEnd + 1) > deviceSize)
            {
                printLog(string.Format("Flash 空间不足 需要{0:d} 剩余{1:d}", fileLength, addrEnd - addrBegin + 1));
                port.Close();
                enableButton();
                return;
            }
            else
            {
                addrEnd = addrBegin + fileLength - 1;
            }

            int mbcType = mbcTypeSelected();

            // 自动擦除flash
            byte[] temp = new byte[512];
            int bank = addrBegin >> 14;
            int addr;

            mbc_romSwitchBank(mbcType, bank);
            addr = mbc_BaseAddressOfBank(mbcType, bank);
            gbcCart_read((UInt32)addr, ref temp);

            if (!isBlank(temp))
            {
                Stopwatch swErase = new Stopwatch();
                swErase.Start();

                mbc5_romEraseSector(addrBegin, addrEnd);

                swErase.Stop();
                printLog(string.Format("擦除耗时 {0:f3} s", swErase.ElapsedMilliseconds / 1000.0f));
            }


            // 开始发送
            printLog("开始写入");
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            int currentBank = -123;
            int writtenCount = 0;

            while (writtenCount < fileLength)
            {
                // 分包
                int sentLen = fileLength - writtenCount;
                sentLen = sentLen > 4096 ? 4096 : sentLen;

                byte[] sendPack = new byte[sentLen];
                Array.Copy(rom, writtenCount, sendPack, 0, sentLen);

                int romAddress = addrBegin + writtenCount;
                int cartAddress;
				
				byte[] buff = new byte[bufferWriteBytes];

                // 切bank
                bank = romAddress >> 14;

                if (bank != currentBank)
                {
                    currentBank = bank;
                    mbc_romSwitchBank(mbcType, bank);
                    //printLog(string.Format("    切换至 bank{0:d}", bank));
                }

                cartAddress = mbc_BaseAddressOfBank(mbcType, bank) + (romAddress & 0x3fff)+256;
				printLog(string.Format("{0:x}",cartAddress));

                // 写入
				int buffcount = 0;
				while (buffcount < 16)
				{
					gbcCart_write((UInt32)cartAddress, new byte[] { 0x41 }); // Page program start
					Array.Copy(sendPack,buffcount*bufferWriteBytes,buff,0,bufferWriteBytes);
					gbcCart_write((UInt32)cartAddress, buff);
					buffcount ++;
					printLog(string.Format("{0:d}",buffcount));
					
					cartAddress += 256;
					printLog(string.Format("{0:x}",cartAddress));
					//gbcCart_romProgram((UInt32)cartAddress, sendPack, (UInt16)bufferWriteBytes);
				}

                writtenCount += sentLen;
                showProgress(writtenCount, fileLength);
            }

            gbcCart_write(0x4000, new byte[] { 0x00 }); // 防止震动

            stopwatch.Stop();

            if (pwr_5v_enabled)
            {
                pwr_5v_enabled = false;
                cart_power(true, false); // 3.3v out
                printLog("关闭5V");
            }
            port.Close();
            enableButton();

            printScore(fileLength, stopwatch.ElapsedMilliseconds);
        }

        void mission_dumpRom_mbc5()
        {
            string romFilePath = textBox_romPath.Text; // 打开文件

            //FileStream file;
            try
            {
                file = new FileStream(romFilePath, FileMode.Create, FileAccess.Write);
            }
            catch
            {
                printLog("文件被占用");
                port.Close();
                enableButton();
                return;
            }

            int fileLength = (int)(double.Parse(comboBox_romSize_mbc5.Text) * 1024 * 1024);
            byte[] rom = new byte[fileLength];

            // 切换电源
            if (checkBox_mbc5V.Checked)
            {
                printLog("输出5V");
                pwr_5v_enabled = true;
                cart_power(false, false); // power down
                Thread.Sleep(111);
                cart_power(true, true); // 5v out
                Thread.Sleep(333);
            }

            // 获取rom flash buffer大小
            int deviceSize, bufferWriteBytes;
            mbc5_romGetSize(out bufferWriteBytes, out deviceSize);

            // 获取工作区间
            int addrBegin, addrEnd;
            int gameSelect = comboBox_mbc5MultiCartSelect.SelectedIndex;
            if (gameSelect != 0)
            {
                addrBegin = (int)mbc5_multiCardAddrRange_rom[gameSelect, 0];
                addrEnd = (int)mbc5_multiCardAddrRange_rom[gameSelect, 1];
            }
            else
            {
                addrBegin = 0;
                addrEnd = fileLength - 1;
            }

            if (addrEnd > deviceSize - 1)
            {
                fileLength = deviceSize - addrBegin;
                if (fileLength <= 0)
                {
                    printLog("该地址无数据");
                    file.Close();
                    port.Close();
                    enableButton();
                    return;
                }
            }

            int mbcType = mbcTypeSelected();

            // 开始读取
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            int currentBank = -123;
            int readCount = 0;

            while (readCount < fileLength)
            {
                // 分包
                int readLen = fileLength - readCount;
                readLen = readLen > 4096 ? 4096 : readLen;

                int romAddress = addrBegin + readCount;
                int cartAddress;

                // 切bank 
                int bank = romAddress >> 14;

                if (bank != currentBank)
                {
                    currentBank = bank;
                    mbc_romSwitchBank(mbcType, bank);
                    //printLog(string.Format("    切换至 bank{0:d}", bank));
                }

                cartAddress = mbc_BaseAddressOfBank(mbcType, bank) + (romAddress & 0x3fff);

                // 读取
                byte[] respon = new byte[readLen];
                gbcCart_read((UInt32)cartAddress, ref respon);

                Array.Copy(respon, 0, rom, readCount, readLen);

                readCount += readLen;
                showProgress(readCount, fileLength);
            }
            stopwatch.Stop();

            // 保存

            file.Write(rom, 0, fileLength);
            file.Close();

            if (pwr_5v_enabled)
            {
                pwr_5v_enabled = false;
                cart_power(true, false); // 3.3v out
                printLog("关闭5V");
            }
            port.Close();
            enableButton();

            printScore(fileLength, stopwatch.ElapsedMilliseconds);
        }

        void mission_verifyRom_mbc5()
        {
            // 打开文件
            string romFilePath = textBox_romPath.Text;

            //FileStream file;
            try
            {
                file = new FileStream(romFilePath, FileMode.Open, FileAccess.Read);
            }
            catch
            {
                printLog("文件被占用");
                port.Close();
                enableButton();
                return;
            }

            int fileLength = (int)file.Length;

            byte[] rom = new byte[fileLength];
            file.Read(rom, 0, fileLength);
            file.Close();

            // 切换电源
            if (checkBox_mbc5V.Checked)
            {
                printLog("输出5V");
                pwr_5v_enabled = true;
                cart_power(false, false); // power down
                Thread.Sleep(111);
                cart_power(true, true); // 5v out
                Thread.Sleep(333);
            }

            // 获取工作区间
            int addrBegin, addrEnd;
            int gameSelect = comboBox_mbc5MultiCartSelect.SelectedIndex;
            if (gameSelect != 0)
            {
                addrBegin = (int)mbc5_multiCardAddrRange_rom[gameSelect, 0];
                addrEnd = (int)mbc5_multiCardAddrRange_rom[gameSelect, 1];
            }
            else
            {
                addrBegin = 0;
                addrEnd = fileLength - 1;
            }

            int mbcType = mbcTypeSelected();

            // 开始校验
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            int currentBank = -123;
            int readCount = 0;

            while (readCount < fileLength)
            {
                // 分包
                int readLen = fileLength - readCount;
                readLen = readLen > 4096 ? 4096 : readLen;

                int romAddress = addrBegin + readCount;
                int cartAddress;

                // 切bank
                int bank = romAddress >> 14;

                if (bank != currentBank)
                {
                    currentBank = bank;
                    mbc_romSwitchBank(mbcType, bank);
                    //printLog(string.Format("    切换至 bank{0:d}", bank));
                }

                cartAddress = mbc_BaseAddressOfBank(mbcType, bank) + (romAddress & 0x3fff);

                // 读取
                byte[] respon = new byte[readLen];
                gbcCart_read((UInt32)cartAddress, ref respon);

                // 对比
                for (int i = 0; i < readLen; i++)
                {
                    if (rom[readCount + i] != respon[i])
                    {
                        printLog(string.Format(
                            "0x{0:x8}校验失败，{1:x2} -> {2:x2}",
                            romAddress + i,
                            rom[readCount + i],
                            respon[i]
                        ));
                    }
                }
                readCount += readLen;
                showProgress(readCount, fileLength);
            }
            stopwatch.Stop();

            if (pwr_5v_enabled)
            {
                pwr_5v_enabled = false;
                cart_power(true, false); // 3.3v out
                printLog("关闭5V");
            }
            port.Close();
            enableButton();

            printScore(fileLength, stopwatch.ElapsedMilliseconds);
        }

        void mission_wrtieRam_mbc5()
        {
            // 打开文件
            string savFilePath = textBox_savePath.Text;

            //FileStream file;
            try
            {
                file = new FileStream(savFilePath, FileMode.Open, FileAccess.Read);
            }
            catch
            {
                printLog("文件被占用");
                port.Close();
                enableButton();
                return;
            }

            int fileLength = (int)file.Length;

            byte[] sav = new byte[fileLength];
            file.Read(sav, 0, fileLength);
            file.Close();


            // 切换电源
            if (checkBox_mbc5V.Checked)
            {
                printLog("输出5V");
                pwr_5v_enabled = true;
                cart_power(false, false); // power down
                Thread.Sleep(111);
                cart_power(true, true); // 5v out
                Thread.Sleep(333);
            }

            // 获取工作区间
            int addrBegin, addrEnd;
            int gameSelect = comboBox_mbc5MultiCartSelect.SelectedIndex;
            if (gameSelect != 0)
            {
                addrBegin = (int)mbc5_multiCardAddrRange_ram[gameSelect, 0];
                addrEnd = (int)mbc5_multiCardAddrRange_ram[gameSelect, 1];
            }
            else
            {
                addrBegin = 0;
                addrEnd = fileLength - 1;
            }

            int mbcType = mbcTypeSelected();

            // 开启ram访问权限
            gbcCart_write(0x0000, new byte[] { 0x0a });

            // 开始发送
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            int currentBank = -123;
            int writtenCount = 0;

            while (writtenCount < fileLength)
            {
                // 分包
                int sentLen = fileLength - writtenCount;
                sentLen = sentLen > 4096 ? 4096 : sentLen;

                byte[] sendPack = new byte[sentLen];
                Array.Copy(sav, writtenCount, sendPack, 0, sentLen);

                int ramAddress = addrBegin + writtenCount;
                int cartAddress;

                // 切bank
                int bank = ramAddress >> 13;

                if (bank != currentBank)
                {
                    currentBank = bank;
                    mbc_ramSwitchBank(mbcType, bank);
                    //printLog(string.Format("    切换至 bank{0:d}", bank > 0 ? bank : 0));
                }

                cartAddress = 0xa000 + (ramAddress & 0x1fff);

                // 写入
                if (comboBox_mbc5RamType.Text == "FRAM")
                    gbcCart_write_forFram((UInt32)cartAddress, sendPack, 10);
                else
                    gbcCart_write((UInt32)cartAddress, sendPack);

                writtenCount += sentLen;
                showProgress(writtenCount, fileLength);
            }
            stopwatch.Stop();

            if (pwr_5v_enabled)
            {
                pwr_5v_enabled = false;
                cart_power(true, false); // 3.3v out
                printLog("关闭5V");
            }
            port.Close();
            enableButton();

            printScore(fileLength, stopwatch.ElapsedMilliseconds);
        }

        void mission_dumpRam_mbc5()
        {
            string savFilePath = textBox_savePath.Text; // 打开文件
            //FileStream file;
            try
            {
                file = new FileStream(savFilePath, FileMode.Create, FileAccess.Write);
            }
            catch
            {
                printLog("文件被占用");
                port.Close();
                enableButton();
                return;
            }

            int fileLength = (int)(double.Parse(comboBox_saveSize_mbc5.Text) * 1024);
            byte[] sav = new byte[fileLength];

            // 切换电源
            if (checkBox_mbc5V.Checked)
            {
                printLog("输出5V");
                pwr_5v_enabled = true;
                cart_power(false, false); // power down
                Thread.Sleep(111);
                cart_power(true, true); // 5v out
                Thread.Sleep(333);
            }

            // 获取工作区间
            int addrBegin, addrEnd;
            int gameSelect = comboBox_mbc5MultiCartSelect.SelectedIndex;
            if (gameSelect != 0)
            {
                addrBegin = (int)mbc5_multiCardAddrRange_ram[gameSelect, 0];
                addrEnd = (int)mbc5_multiCardAddrRange_ram[gameSelect, 1];
            }
            else
            {
                addrBegin = 0;
                addrEnd = fileLength - 1;
            }

            int mbcType = mbcTypeSelected();

            // 开启ram访问权限
            gbcCart_write(0x0000, new byte[] { 0x0a });

            // 开始导出
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            int currentBank = -123;
            int readCount = 0;

            while (readCount < fileLength)
            {
                // 分包
                int readLen = fileLength - readCount;
                readLen = readLen > 4096 ? 4096 : readLen;

                int ramAddress = addrBegin + readCount;
                int cartAddress;

                // 切bank
                int bank = ramAddress >> 13;

                if (bank != currentBank)
                {
                    currentBank = bank;
                    mbc_ramSwitchBank(mbcType, bank);
                    //printLog(string.Format("    切换至 bank{0:d}", bank > 0 ? bank : 0));
                }

                cartAddress = 0xa000 + (ramAddress & 0x1fff);

                // 读取
                byte[] respon = new byte[readLen];

                if (comboBox_mbc5RamType.Text == "FRAM")
                    gbcCart_read_forFram((UInt32)cartAddress, ref respon, 10);
                else
                    gbcCart_read((UInt32)cartAddress, ref respon);

                Array.Copy(respon, 0, sav, readCount, readLen);

                readCount += readLen;
                showProgress(readCount, fileLength);
            }
            stopwatch.Stop();

            // 保存
            file.Write(sav, 0, fileLength);
            file.Close();

            if (pwr_5v_enabled)
            {
                pwr_5v_enabled = false;
                cart_power(true, false); // 3.3v out
                printLog("关闭5V");
            }
            port.Close();
            enableButton();

            printScore(fileLength, stopwatch.ElapsedMilliseconds);
        }

        void mission_verifyRam_mbc5()
        {
            // 打开文件
            string savFilePath = textBox_savePath.Text;

            //FileStream file;
            try
            {
                file = new FileStream(savFilePath, FileMode.Open, FileAccess.Read);
            }
            catch
            {
                printLog("文件被占用");
                port.Close();
                enableButton();
                return;
            }

            int fileLength = (int)file.Length;

            byte[] sav = new byte[fileLength];
            file.Read(sav, 0, fileLength);
            file.Close();

            // 切换电源
            if (checkBox_mbc5V.Checked)
            {
                printLog("输出5V");
                pwr_5v_enabled = true;
                cart_power(false, false); // power down
                Thread.Sleep(111);
                cart_power(true, true); // 5v out
                Thread.Sleep(333);
            }

            // 获取工作区间
            int addrBegin, addrEnd;
            int gameSelect = comboBox_mbc5MultiCartSelect.SelectedIndex;
            if (gameSelect != 0)
            {
                addrBegin = (int)mbc5_multiCardAddrRange_ram[gameSelect, 0];
                addrEnd = (int)mbc5_multiCardAddrRange_ram[gameSelect, 1];
            }
            else
            {
                addrBegin = 0;
                addrEnd = fileLength - 1;
            }

            int mbcType = mbcTypeSelected();

            // 开启ram访问权限
            gbcCart_write(0x0000, new byte[] { 0x0a });

            // 开始校验
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            int currentBank = -123;
            int readCount = 0;

            while (readCount < fileLength)
            {
                // 分包
                int readLen = fileLength - readCount;
                readLen = readLen > 4096 ? 4096 : readLen;

                int ramAddress = addrBegin + readCount;
                int cartAddress;

                // 切bank
                int bank = ramAddress >> 13;

                if (bank != currentBank)
                {
                    currentBank = bank;
                    mbc_ramSwitchBank(mbcType, bank);
                    //printLog(string.Format("    切换至 bank{0:d}", bank > 0 ? bank : 0));
                }

                cartAddress = 0xa000 + (ramAddress & 0x1fff);

                // 读取
                byte[] respon = new byte[readLen];

                if (comboBox_mbc5RamType.Text == "FRAM")
                    gbcCart_read_forFram((UInt32)cartAddress, ref respon, 10);
                else
                    gbcCart_read((UInt32)cartAddress, ref respon);

                // 对比
                for (int i = 0; i < readLen; i++)
                {
                    if (sav[readCount + i] != respon[i])
                    {
                        printLog(string.Format(
                            "0x{0:x8}校验失败，{1:x2} -> {2:x2}",
                            ramAddress + i,
                            sav[readCount + i],
                            respon[i]
                        ));
                    }
                }
                readCount += readLen;
                showProgress(readCount, fileLength);
            }
            stopwatch.Stop();

            if (pwr_5v_enabled)
            {
                pwr_5v_enabled = false;
                cart_power(true, false); // 3.3v out
                printLog("关闭5V");
            }
            port.Close();
            enableButton();

            printScore(fileLength, stopwatch.ElapsedMilliseconds);
        }

    }
}
