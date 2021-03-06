﻿using Neo.Tools.AVM;
using Neo.VM;
using Neo.Debugger.Utils;
using System;
using System.Diagnostics;
using System.Text;
using NeoLux;

namespace Neo.Emulator.API
{
    public static class Runtime
    {
        public static KeyPair invokerKeys;

        public static Action<string> OnLogMessage;

        [Syscall("Neo.Runtime.GetTrigger")]
        public static bool GetTrigger(ExecutionEngine engine)
        {
            TriggerType result = TriggerType.Application;

            engine.EvaluationStack.Push((int)result);
            return true;
        }

        [Syscall("Neo.Runtime.CheckWitness", 0.2)]
        public static bool CheckWitness(ExecutionEngine engine)
        {
            byte[] hashOrPubkey = engine.EvaluationStack.Pop().GetByteArray();

            bool result;

            string matchType;

            if (hashOrPubkey.Length == 20) // script hash
            {
                matchType = "Script Hash";
                result = true;
            }
            else if (hashOrPubkey.Length == 33) // public key
            {
                matchType = "Public Key";

                if (invokerKeys != null)
                {
                    result = invokerKeys.CompressedPublicKey.ByteMatch(hashOrPubkey);
                }
                else
                {
                    result = false;
                }
            }
            else
            {
                matchType = "Unknown";
                result = false;
            }

            DoLog($"Checking Witness [{matchType}]: {hashOrPubkey.ByteToHex()} => {result}");

            engine.EvaluationStack.Push(new VM.Types.Boolean(result));
            return true;
        }

        [Syscall("Neo.Runtime.Notify")]
        public static bool Notify(ExecutionEngine engine)
        {
            //params object[] state
            var something = engine.EvaluationStack.Pop();

            if (something.IsArray)
            {
                var sb = new StringBuilder();

                var items = something.GetArray();

                int index = 0;

                foreach (var item in items)
                {
                    if (index > 0)
                    {
                        sb.Append(" / ");
                    }

                    sb.Append(FormattingUtils.StackItemAsString(item, true));
                    index++;
                }

                DoLog(sb.ToString());
                return true;
            }
            else
            {
                return false;
            }
        }

        [Syscall("Neo.Runtime.Log")]
        public static bool Log(ExecutionEngine engine)
        {
            var msg = engine.EvaluationStack.Pop();
            DoLog(FormattingUtils.StackItemAsString(msg));
            return true;
        }

        private static void DoLog(string msg)
        {
            Debug.WriteLine(msg);

            if (OnLogMessage != null)
            {
                OnLogMessage(msg);
            }
        }
    }
}
