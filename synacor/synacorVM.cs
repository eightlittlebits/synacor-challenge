using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace synacor
{
    class SynacorVM
    {
        private ushort _pc = 0;

        private ushort[] _memory = new ushort[32768];
        private ushort[] _registers = new ushort[8];
        private Stack<ushort> _stack = new Stack<ushort>();

        const ushort MaxLiteral = 32768;

        private bool _running = false;

        private string[] _mnemonics =
        {
            "halt", "set", "push", "pop", "eq", "gt", "jmp", "jt", "jf",
            "add", "mult", "mod", "and", "or", "not", "rmem", "wmem",
            "call", "ret", "out", "in", "noop"
        };

        private string _readBuffer = string.Empty;
        private int _readPosition = 0;

        public SynacorVM()
        {

        }

        internal void Load(ushort[] challengeData)
        {
            // load into memory
            Array.Copy(challengeData, _memory, challengeData.Length);
        }

        // run until we hit a halt opcode
        internal void Run()
        {
            _running = true;

            while (_running)
            {
                Execute();
            }
        }

        // convert from a value read from memory into a literal or register read
        private ushort RegisterLiteral(ushort value)
        {
            // values <= 32767 return the literal value
            if (value <= 32767)
            {
                return value;
            }
            // values >= 32768 and <= 32755 return value in register 0-7
            else if (value <= 32775)
            {
                return _registers[value % MaxLiteral];
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(value),
                    value,
                    "Register/Literal value must be <= 32775");
            }
        }

        private void RegisterWrite(ushort register, ushort value)
        {
            Debug.Assert(register >= 32768 && register <= 32775);

            _registers[register % MaxLiteral] = value;
        }

        // execute a single step
        internal void Execute()
        {
            ushort executing = _pc;

            // retrieve next opcode and advance pc
            ushort opcode = _memory[_pc++];

            switch (opcode)
            {
                // halt: 0
                //   stop execution and terminate the program
                case 0:
                    _running = false;
                    break;

                // set: 1 a b
                //   set register <a> to the value of<b>
                case 1:
                    {
                        ushort a = _memory[_pc++];
                        ushort b = _memory[_pc++];

                        RegisterWrite(a, RegisterLiteral(b));
                    }
                    break;

                // push: 2 a
                //   push <a> onto the stack
                case 2:
                    {
                        ushort a = _memory[_pc++];
                        _stack.Push(RegisterLiteral(a));
                    }
                    break;

                // pop: 3 a
                //   remove the top element from the stack and write it into <a>; empty stack = error
                case 3:
                    {
                        if (_stack.Count == 0)
                        {
                            Console.WriteLine("Pop on empty stack");
                            _running = false;
                        }

                        ushort a = _memory[_pc++];
                        RegisterWrite(a, _stack.Pop());
                    } break;

                // eq: 4 a b c
                //   set<a> to 1 if < b > is equal to <c>; set it to 0 otherwise
                case 4:
                    {
                        ushort a = _memory[_pc++];
                        ushort b = _memory[_pc++];
                        ushort c = _memory[_pc++];

                        RegisterWrite(a, (ushort)(RegisterLiteral(b) == RegisterLiteral(c) ? 1 : 0));
                    }
                    break;

                // gt: 5 a b c
                //   set <a> to 1 if <b> is greater than <c>; set it to 0 otherwise
                case 5:
                    {
                        ushort a = _memory[_pc++];
                        ushort b = _memory[_pc++];
                        ushort c = _memory[_pc++];

                        RegisterWrite(a, (ushort)(RegisterLiteral(b) > RegisterLiteral(c) ? 1 : 0));
                    }
                    break;


                // jmp: 6 a
                //   jump to <a>
                case 6:
                    {
                        ushort a = _memory[_pc];
                        _pc = RegisterLiteral(a);
                    }
                    break;

                // jt: 7 a b
                //   if <a> is nonzero, jump to <b>
                case 7:
                    {
                        ushort a = _memory[_pc++];
                        ushort b = _memory[_pc++];

                        if (RegisterLiteral(a) != 0)
                        {
                            _pc = RegisterLiteral(b);
                        }
                    }
                    break;

                // jf: 8 a b
                //   if <a> is zero, jump to <b>
                case 8:
                    {
                        ushort a = _memory[_pc++];
                        ushort b = _memory[_pc++];

                        if (RegisterLiteral(a) == 0)
                        {
                            _pc = RegisterLiteral(b);
                        }
                    }
                    break;

                // add: 9 a b c
                //     assign into <a> the sum of <b> and <c> (modulo 32768)
                case 9:
                    {
                        ushort a = _memory[_pc++];
                        ushort b = _memory[_pc++];
                        ushort c = _memory[_pc++];

                        RegisterWrite(a, (ushort)((RegisterLiteral(b) + RegisterLiteral(c)) % MaxLiteral));
                    }
                    break;


                // mult: 10 a b c
                //   store into <a> the product of <b> and <c> (modulo 32768)
                case 10:
                    {
                        ushort a = _memory[_pc++];
                        ushort b = _memory[_pc++];
                        ushort c = _memory[_pc++];

                        RegisterWrite(a, (ushort)((RegisterLiteral(b) * RegisterLiteral(c)) % MaxLiteral));
                    }
                    break;

                // mod: 11 a b c
                //   store into <a> the remainder of <b> divided by <c>
                case 11:
                    {
                        ushort a = _memory[_pc++];
                        ushort b = _memory[_pc++];
                        ushort c = _memory[_pc++];

                        RegisterWrite(a, (ushort)(RegisterLiteral(b) % RegisterLiteral(c)));
                    }
                    break;


                // and: 12 a b c
                //    stores into<a> the bitwise and of <b> and < c >
                case 12:
                    {
                        ushort a = _memory[_pc++];
                        ushort b = _memory[_pc++];
                        ushort c = _memory[_pc++];

                        RegisterWrite(a, (ushort)(RegisterLiteral(b) & RegisterLiteral(c)));
                    }
                    break;


                // or: 13 a b c
                //  stores into <a> the bitwise or of <b> and <c>
                case 13:
                    {
                        ushort a = _memory[_pc++];
                        ushort b = _memory[_pc++];
                        ushort c = _memory[_pc++];

                        RegisterWrite(a, (ushort)(RegisterLiteral(b) | RegisterLiteral(c)));
                    }
                    break;


                // not: 14 a b
                //   stores 15 - bit bitwise inverse of <b> in <a>
                case 14:
                    {
                        ushort a = _memory[_pc++];
                        ushort b = _memory[_pc++];

                        RegisterWrite(a, (ushort)(~RegisterLiteral(b) & 0x7FFF));
                    }
                    break;

                // rmem: 15 a b
                //   read memory at address <b> and write it to <a>
                case 15:
                    {
                        ushort a = _memory[_pc++];
                        ushort b = _memory[_pc++];

                        RegisterWrite(a, _memory[RegisterLiteral(b)]);
                    }
                    break;

                // wmem: 16 a b
                //   write the value from <b> into memory at address <a>
                case 16:
                    {
                        ushort a = _memory[_pc++];
                        ushort b = _memory[_pc++];

                        _memory[RegisterLiteral(a)] = RegisterLiteral(b);
                    }
                    break;


                // call: 17 a
                //   write the address of the next instruction to the stack and jump to <a>
                case 17:
                    {
                        ushort a = _memory[_pc++];

                        _stack.Push(_pc);

                        _pc = RegisterLiteral(a);
                    }
                    break;


                // ret: 18
                //   remove the top element from the stack and jump to it; empty stack = halt
                case 18:
                    {
                        if (_stack.Count == 0)
                        {
                            _running = false;
                        }
                        
                        _pc = _stack.Pop();
                    }
                    break;

                // out: 19 a
                //   write the character represented by ascii code <a> to the terminal
                case 19:
                    {
                        ushort a = _memory[_pc++];
                        Console.Write((char)RegisterLiteral(a));
                    }
                    break;

                // in: 20 a
                //   read a character from the terminal and write its ascii code to <a>; it can be assumed
                //   that once input starts, it will continue until a newline is encountered; this means
                //   that you can safely read whole lines from the keyboard and trust that they will be
                //   fully read
                case 20:
                    {
                        // if the read buffer is empty then read a line from input
                        if (_readBuffer == string.Empty)
                        {
                            _readBuffer = Console.ReadLine();
                        }

                        ushort a = _memory[_pc++];

                        // if we're in the middle of a string read the continue returning chars
                        if (_readPosition < _readBuffer.Length)
                        {
                            RegisterWrite(a, _readBuffer[_readPosition++]);
                        }
                        else // we've finished reading the string, return newline and reset
                        {
                            RegisterWrite(a, '\n');

                            _readBuffer = string.Empty;
                            _readPosition = 0;
                        }
                    }
                    break;

                // noop: 21
                //   no operation
                case 21:
                    break;

                default:
                    Console.WriteLine($"Unimplemented opcode {opcode} at address {_pc - 1}");
                    _running = false;
                    break;
            }



        }
    }
}
