using Datalaag;
using Globals;

namespace Logica {
    public class BfInterpreter {
        private FileLoader fileLoader;
        /// <see cref="https://www.tutorialsteacher.com/csharp/csharp-stack"></see>
        private Stack<int> loopPointer;
        private int inputPointer;
        private List<byte> memory;//external read only

        /// <see cref="https://learn.microsoft.com/en-us/dotnet/api/microsoft.sqlserver.management.sdk.sfc.ireadonlylist-1?view=sql-smo-160"</see>
        public IReadOnlyList<byte> MemoryView { get; }
        public Int16 MemoryPointer { get; private set; }
        public Programdata Program { get; private set; }
        public string PreparedInput { get; set; }
        public Func<char> InputFunction { private get; set; }
        public Action<string> OutputFunction { private get; set; }
        public Action Tick { private get; set; }
        public int ProgramPointer { get; private set; }

        /// <summary>
        /// setup interne parameters voor programma
        /// </summary>
        /// <param name="inputFunction">funcie die wordt uitgeroepen bij "," commando</param>
        /// <param name="outputFunction">funcie die wordt uitgeroepen bij "." commando</param>
        public BfInterpreter(Func<char> inputFunction, Action<string> outputFunction) {
            ProgramPointer = 0;
            this.fileLoader = new FileLoader();
            this.memory = new List<byte>(Int16.MaxValue);
            for (int i = 0; i < Int16.MaxValue; i++) this.memory.Add(0);
            this.MemoryView = memory.AsReadOnly();
            this.MemoryPointer = 0;
            this.loopPointer = new Stack<int>();
            this.inputPointer = 0;

            this.InputFunction = inputFunction;
            this.OutputFunction = outputFunction;
            this.Program = new Programdata("");
            this.PreparedInput = "";
            this.Tick = () => { };
        }

        /// <summary>
        /// laad een brainfuck programma van string of file
        /// </summary>
        /// <param name="programinput"></param>
        /// <see cref="https://stackoverflow.com/questions/3137097/check-if-a-string-is-a-valid-windows-directory-folder-path"></see>
        public void LoadProgram(string programinput) {
            string loadedProgram = "";
            try {
                Path.GetFullPath(programinput);
                loadedProgram = this.fileLoader.Load(programinput);
            }
            catch (FileNotFoundException) {
                loadedProgram = programinput;
            }
            catch (Exception e) {
                Console.WriteLine(e.ToString());
                this.OutputFunction("file " + programinput + " could not be found");
            }
            this.Program = new Programdata(loadedProgram);
        }

        public void Interpret() {
            while (ProgramPointer < this.Program.Length) {
                Tick();
                Commands cmd = Program.Compiled[ProgramPointer];
                Step(cmd);
                ProgramPointer++;
            }
        }

        public void Step() {
            Commands cmd = Program.Compiled[ProgramPointer];
            Step(cmd);
        }

        /// <summary>
        /// het uitvoeren van het brainfuck programma gebeurt hier
        /// </summary>
        /// <see cref="https://www.w3schools.com/cs/cs_switch.php"/>
        /// <see cref="https://www.c-sharpcorner.com/UploadFile/mahesh/convert-char-to-byte-in-C-Sharp/"></see>
        private void Step(Commands cmd) {
            if (MemoryPointer < 0 || MemoryPointer > this.MemoryView.Count) throw new IndexOutOfRangeException("tried to access index " + MemoryPointer.ToString() + " of memory with size " + this.MemoryView.Count.ToString());
            switch (cmd) {
                case Commands.Inc:
                    this.memory[MemoryPointer]++;
                    break;
                case Commands.Dec:
                    this.memory[MemoryPointer]--;
                    break;
                case Commands.Right:
                    this.MemoryPointer++;
                    break;
                case Commands.Left:
                    this.MemoryPointer--;
                    break;
                case Commands.Loop:
                    if (this.MemoryView[MemoryPointer] != 0) {
                        this.loopPointer.Push(ProgramPointer);
                    }
                    else {
                        //find index of matching ], dit codeblok hoort namelijk niet te worden uitgevoerd
                        int bracketCount = 0;
                        int index = 0;
                        for (int j = ProgramPointer; j < this.Program.Length; j++) {
                            if (this.Program.Compiled[j] == Commands.Loop) bracketCount++;
                            if (this.Program.Compiled[j] == Commands.Jmp) bracketCount--;
                            index = j;
                            if (bracketCount == 0) break;
                        }
                        ProgramPointer = index;
                    }
                    break;
                case Commands.Jmp:
                    if (this.MemoryView[MemoryPointer] != 0) ProgramPointer = this.loopPointer.Pop() - 1;
                    if (this.MemoryView[MemoryPointer] == 0) this.loopPointer.Pop();
                    break;
                case Commands.Read:
                    if (inputPointer < this.PreparedInput.Length) {
                        this.memory[MemoryPointer] = Convert.ToByte(this.PreparedInput[inputPointer]);
                    }
                    else {
                        this.memory[MemoryPointer] = Convert.ToByte(InputFunction());
                    }
                    break;
                case Commands.Write:
                    string outp = ((char)this.MemoryView.ElementAt(MemoryPointer)).ToString();
                    OutputFunction(outp);
                    break;
            }
        }
    }
}
