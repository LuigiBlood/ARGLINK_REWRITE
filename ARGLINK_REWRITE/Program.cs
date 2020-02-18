using System;
using System.Collections.Generic;
using System.IO;

namespace ARGLINK_REWRITE
{
    class Program
    {
        struct LinkData
        {
            public string name;
            public int value;
        }

        struct Calculation
        {
            public int deep;
            public int priority;
            public int operation;
            public int value;
        }

        static void Main(string[] args)
        {
            Console.WriteLine("ARGLINK REWRITE\nby LuigiBlood\n----------------");
            for (int i = 0; i < args.Length; i++)
                Console.WriteLine(args[i]);
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: ARGLINK <ROM OUTPUT> <SOB INPUT FILE(S)>\n");
            }
            else
            {
                BinaryWriter FileOut;
                BinaryReader FileSOB;
                BinaryReader FileExt;

                //Fill Output file to 1MB
                FileOut = new BinaryWriter(File.OpenWrite(args[0]));
                FileOut.Seek(0, SeekOrigin.Begin);
                for (int i = 0; i < 0x100000; i++)
                {
                    FileOut.BaseStream.WriteByte(0xFF);
                }

                //SOB files, Step 1 & 2 - input all data and list all links
                List<LinkData> link = new List<LinkData>();
                int SOBInput = args.Length - 1;
                long[] startLink = new long[SOBInput];

                for (int idx = 0; idx < SOBInput; idx++)
                {
                    //Check if SOB file is indeed a SOB file
                    int count = 0;
                    FileSOB = new BinaryReader(File.OpenRead(args[idx + 1]));
                    Console.WriteLine("Open " + args[idx + 1]);
                    FileSOB.BaseStream.Seek(0, SeekOrigin.Begin);
                    if (FileSOB.ReadByte() == 0x53      //S
                        && FileSOB.ReadByte() == 0x4F   //O
                        && FileSOB.ReadByte() == 0x42   //B
                        && FileSOB.ReadByte() == 0x4A)  //J
                    {
                        FileSOB.ReadByte();
                        FileSOB.ReadByte();
                        count = FileSOB.ReadByte();
                        FileSOB.ReadByte();

                        for (int i = 0; i < count; i++)
                        {
                            //Step 1 - Input all data into output
                            long start = FileSOB.BaseStream.Position;
                            int offset = (FileSOB.ReadByte() << 0) | (FileSOB.ReadByte() << 8) | (FileSOB.ReadByte() << 16) | (FileSOB.ReadByte() << 24);
                            int size = (FileSOB.ReadByte() << 0) | (FileSOB.ReadByte() << 8) | (FileSOB.ReadByte() << 16) | (FileSOB.ReadByte() << 24);
                            int type = FileSOB.ReadByte();

                            Console.WriteLine(i.ToString("X") + ": 0x" + start.ToString("X") + "  /// Size: 0x" + size.ToString("X") + " / Offset 0x" + offset.ToString("X") + " / Type " + type.ToString("X"));
                            byte[] buffer = new byte[size];

                            if (type == 0)
                            {
                                //Data
                                FileSOB.Read(buffer, 0, size);
                                FileOut.Seek(offset, SeekOrigin.Begin);
                                FileOut.Write(buffer, 0, size);
                            }
                            else if (type == 1)
                            {
                                //External File
                                FileSOB.ReadByte();
                                FileSOB.ReadByte();

                                //Get filepath
                                List<char> filepath = new List<char>();
                                char check = 'A';
                                while (check != 0)
                                {
                                    check = FileSOB.ReadChar();
                                    if (check != 0)
                                        filepath.Add(check);
                                }

                                Console.WriteLine("--Open External File: " + String.Concat(filepath));

                                FileExt = new BinaryReader(File.OpenRead(String.Concat(filepath)));
                                FileExt.Read(buffer, 0, size);
                                FileOut.Seek(offset, SeekOrigin.Begin);
                                FileOut.Write(buffer, 0, size);

                                FileExt.Close();
                            }
                            else
                            {
                                //Unknown, shouldn't happen
                            }
                        }

                        //Step 2 - Get all extern names and values
                        do
                        {
                            LinkData linktemp = new LinkData();

                            List<char> nametemp = new List<char>();
                            char check = 'A';
                            while (check != 0)
                            {
                                check = FileSOB.ReadChar();
                                if (check != 0)
                                    nametemp.Add(check);
                            }

                            if (nametemp.Count <= 0)
                            {
                                break;
                            }

                            linktemp.name = String.Concat(nametemp);
                            linktemp.value = FileSOB.ReadUInt16() | (FileSOB.ReadByte() << 16);
                            Console.WriteLine("--" + linktemp.name + " : " + linktemp.value.ToString("X"));
                            link.Add(linktemp);
                        } while (FileSOB.ReadByte() == 0);

                        startLink[idx] = FileSOB.BaseStream.Position;
                        FileSOB.Close();
                        //Repeat
                    }
                }

                //Step 3 - Link everything
                Console.WriteLine("----LINK");
                for (int idx = 0; idx < SOBInput; idx++)
                {
                    FileSOB = new BinaryReader(File.OpenRead(args[idx + 1]));
                    Console.WriteLine("Open " + args[idx + 1]);
                    FileSOB.BaseStream.Seek(0, SeekOrigin.Begin);
                    if (FileSOB.ReadByte() == 0x53      //S
                        && FileSOB.ReadByte() == 0x4F   //O
                        && FileSOB.ReadByte() == 0x42   //B
                        && FileSOB.ReadByte() == 0x4A)  //J
                    {
                        if (startLink[idx] < (FileSOB.BaseStream.Length - 3))
                        {
                            Console.WriteLine(startLink[idx].ToString("X"));
                            FileSOB.BaseStream.Seek(startLink[idx], SeekOrigin.Begin);
                            while (FileSOB.BaseStream.Position < FileSOB.BaseStream.Length - 1)
                            {
                                //FileSOB.BaseStream.Seek(-1, SeekOrigin.Current);
                                Console.WriteLine("-" + FileSOB.BaseStream.Position.ToString("X"));
                                //Get name
                                List<char> nametemp = new List<char>();
                                char check = 'A';
                                while (check != 0)
                                {
                                    check = FileSOB.ReadChar();
                                    if (check != 0)
                                        nametemp.Add(check);
                                }

                                string name = String.Concat(nametemp);
                                string prevname = "";

                                //search
                                int name_id = -1;
                                int prevname_id = -1;
                                for (int i = 0; i < link.Count; i++)
                                {
                                    if (link[i].name.Equals(name))
                                        name_id = i;
                                }

                                List<Calculation> linkcalc = new List<Calculation>();
                                Calculation calctemp = new Calculation();
                                calctemp.deep = -1;
                                calctemp.priority = 0;
                                calctemp.operation = 0;
                                calctemp.value = link[name_id].value;
                                linkcalc.Add(calctemp);

                                Console.WriteLine("--" + name + " : " + link[name_id].value.ToString("X"));

                                if (FileSOB.ReadByte() != 0)
                                {
                                    FileSOB.BaseStream.Seek(-1, SeekOrigin.Current);

                                    prevname = name;
                                    prevname_id = name_id;

                                    nametemp = new List<char>();
                                    check = 'A';
                                    while (check != 0)
                                    {
                                        check = FileSOB.ReadChar();
                                        if (check != 0)
                                            nametemp.Add(check);
                                    }

                                    name = String.Concat(nametemp);

                                    for (int i = 0; i < link.Count; i++)
                                    {
                                        if (link[i].name.Equals(name))
                                            name_id = i;
                                    }

                                    Console.WriteLine("----" + name + " : " + link[name_id].value.ToString("X"));
                                    FileSOB.ReadByte();
                                }

                                FileSOB.ReadInt32();
                                FileSOB.ReadInt32();

                                //List all operations
                                byte calccheck1 = FileSOB.ReadByte();
                                byte calccheck2 = FileSOB.ReadByte();
                                while (calccheck1 != 0 && calccheck2 != 0)
                                {
                                    calctemp = new Calculation();
                                    calctemp.deep = (calccheck1 & 0x70) >> 4;
                                    calctemp.priority = (calccheck1 & 0x3);
                                    calctemp.operation = calccheck2;
                                    calctemp.value = FileSOB.ReadInt32();
                                    if (calccheck1 > 0x80)
                                        calctemp.value = link[name_id].value;

                                    calccheck1 = FileSOB.ReadByte();
                                    calccheck2 = FileSOB.ReadByte();
                                    linkcalc.Add(calctemp);
                                }

                                //All operations have been found, now do the calculations
                                while (linkcalc.Count > 1)
                                {
                                    //Check for highest deep
                                    int highestdeep = -1;
                                    int highestdeepidx = -1;
                                    for (int i = 1; i < linkcalc.Count; i++)
                                    {
                                        //Get the first highest one
                                        if (highestdeep < linkcalc[i].deep)
                                        {
                                            highestdeep = linkcalc[i].deep;
                                            highestdeepidx = i;
                                        }
                                    }

                                    //Check for highest priority
                                    int highestpri = -1;
                                    int highestpriidx = -1;
                                    for (int i = highestdeepidx; i < linkcalc.Count; i++)
                                    {
                                        //Get the first highest one
                                        if (linkcalc[i].deep != highestdeep || highestpri > linkcalc[i].priority)
                                            break;
                                        if (highestpri < linkcalc[i].priority && linkcalc[i].deep == highestdeep)
                                        {
                                            highestpri = linkcalc[i].priority;
                                            highestpriidx = i;
                                        }
                                    }

                                    //Check for latest deep
                                    int calcidx = -1;
                                    for (int i = highestpriidx; i >= 0; i--)
                                    {
                                        //Get the first one that comes
                                        if (highestdeep > linkcalc[i].deep || highestpri > linkcalc[i].priority)
                                        {
                                            calcidx = i;
                                            break;
                                        }
                                    }

                                    //Do the calculation
                                    calctemp = linkcalc[calcidx];

                                    switch (linkcalc[highestpriidx].operation)
                                    {
                                        case 0x02:
                                            //Shift Right
                                            Console.WriteLine(calctemp.value.ToString("X") + " >> " + linkcalc[highestpriidx].value.ToString("X"));
                                            calctemp.value >>= linkcalc[highestpriidx].value;
                                            break;
                                        case 0x0C:
                                            //Add
                                            Console.WriteLine(calctemp.value.ToString("X") + " + " + linkcalc[highestpriidx].value.ToString("X"));
                                            calctemp.value += linkcalc[highestpriidx].value;
                                            break;
                                        case 0x0E:
                                            //Sub
                                            Console.WriteLine(calctemp.value.ToString("X") + " - " + linkcalc[highestpriidx].value.ToString("X"));
                                            calctemp.value -= linkcalc[highestpriidx].value;
                                            break;
                                        case 0x10:
                                            //Mul
                                            Console.WriteLine(calctemp.value.ToString("X") + " * " + linkcalc[highestpriidx].value.ToString("X"));
                                            calctemp.value *= linkcalc[highestpriidx].value;
                                            break;
                                        case 0x12:
                                            //Div
                                            Console.WriteLine(calctemp.value.ToString("X") + " / " + linkcalc[highestpriidx].value.ToString("X"));
                                            calctemp.value /= linkcalc[highestpriidx].value;
                                            break;
                                        case 0x16:
                                            //And
                                            Console.WriteLine(calctemp.value.ToString("X") + " & " + linkcalc[highestpriidx].value.ToString("X"));
                                            calctemp.value &= linkcalc[highestpriidx].value;
                                            break;
                                        default:
                                            Console.WriteLine("ERROR (CALCULATION) [" + linkcalc[highestpriidx].operation.ToString("X") + "]");
                                            break;
                                    }
                                    linkcalc[calcidx] = calctemp;

                                    linkcalc.RemoveAt(highestpriidx);
                                }

                                //And then put the data in
                                int offset = FileSOB.ReadInt32();
                                FileOut.Seek(offset + 1, SeekOrigin.Begin);
                                Console.WriteLine("----" + offset.ToString("X") + " : " + linkcalc[0].value.ToString("X"));
                                switch (FileSOB.ReadByte())
                                {
                                    case 0x00:
                                        //8-bit
                                        FileOut.Write((byte)linkcalc[0].value);
                                        break;
                                    case 0x02:
                                        //16-bit
                                        FileOut.Write((UInt16)linkcalc[0].value);
                                        break;
                                    case 0x04:
                                        //24-bit
                                        FileOut.Write((UInt16)linkcalc[0].value);
                                        FileOut.Write((byte)(linkcalc[0].value >> 16));
                                        break;

                                    case 0x0E:
                                        //8-bit
                                        FileOut.Seek(offset, SeekOrigin.Begin);
                                        FileOut.Write((byte)linkcalc[0].value);
                                        break;
                                    case 0x10:
                                        //16-bit
                                        FileOut.Seek(offset, SeekOrigin.Begin);
                                        FileOut.Write((UInt16)linkcalc[0].value);
                                        break;
                                    default:
                                        Console.WriteLine("ERROR (OUTPUT)");
                                        break;
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine("NOTHING");
                        }
                    }
                }
            }
        }
    }
}
