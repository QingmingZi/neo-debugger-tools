﻿using LunarParser;
using LunarParser.JSON;
using Neo.Debugger.Utils;
using Neo.Emulator.API;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Windows.Forms;

namespace Neo.Debugger
{
    public partial class RunForm : Form
    {
        public NeoDebugger debugger;

        private string lastParams = null;

        public RunForm()
        {
            InitializeComponent();

            assetListBox.Items.Clear();
            assetListBox.Items.Add("None");
            foreach (var entry in Asset.Entries)
            {
                assetListBox.Items.Add(entry.name);
            }
            assetListBox.SelectedIndex = 0;
        }

        private void LoadInvokeTemplate(string key)
        {
            if (_paramMap.ContainsKey(key))
            {
                var node = _paramMap[key];

                var json = JSONWriter.WriteToString(node);
                contractInputField.Text = json;

                lastParams = key;
            }
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            var key = paramsList.Text;
            LoadInvokeTemplate(key);
        }

        private bool InitInvoke()
        {
            var json = contractInputField.Text;

            if (string.IsNullOrEmpty(json))
            {
                MessageBox.Show("Invalid input!");
                return false;
            }

            DataNode node;

            try
            {
                node = JSONReader.ReadFromString(json);
            }
            catch
            {
                MessageBox.Show("Error parsing input!");
                return false;
            }

            var items = node.GetNode("params");

            debugger.ContractArgs.Clear();
            foreach (var item in items.Children)
            {
                // TODO - auto convert value to proper types, currently everything is assumed to be strings!

                var obj = ConvertArgument(item);
                debugger.ContractArgs.Add(obj);
            }

            debugger.ClearTransactions();
            if (assetListBox.SelectedIndex > 0)
            {
                foreach (var entry in Asset.Entries)
                {
                    if (entry.name == assetListBox.SelectedItem.ToString())
                    {
                        BigInteger ammount;

                        BigInteger.TryParse(assetAmmount.Text, out ammount);

                        if (ammount > 0)
                        {
                            debugger.AddTransaction(entry.id, ammount);
                        }
                        else
                        {
                            MessageBox.Show(entry.name + " ammount must be greater than zero");
                            return false;
                        }

                        break;
                    }
                }
            }

            return true;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (InitInvoke())
            {
                this.DialogResult = DialogResult.OK;
            }
        }

        private object ConvertArgument(DataNode item)
        {
            if (item.HasChildren)
            {
                var list = new List<object>();
                foreach (var child in item.Children)
                {
                    list.Add(ConvertArgument(child));
                }
                return list;
            }

            BigInteger intVal;

            if (item.Kind == NodeKind.Numeric)
            {                
                if (BigInteger.TryParse(item.Value, out intVal))
                {
                    return intVal;
                }                
                else
                {
                    return 0;
                }
            }
            else
            if (item.Kind == NodeKind.Boolean)
            {
                return "true".Equals(item.Value.ToLowerInvariant()) ? true : false;
            }
            else
            if (item.Kind == NodeKind.Null)
            {
                return null;
            }
            else
            if (item.Value.StartsWith("0x"))
            {
                return item.Value.Substring(2).HexToByte();
            }
            else
            {
                return item.Value;
            }
        }

        private static Dictionary<string, DataNode> _paramMap = null;

        private void RunForm_Shown(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.None;

            assetAmmount.Enabled = assetListBox.SelectedIndex > 0;

            if (Runtime.invokerKeys == null && File.Exists("last.key"))
            {
                var privKey = File.ReadAllBytes("last.key");
                if (privKey.Length == 32)
                {
                    Runtime.invokerKeys = new NeoLux.KeyPair(privKey);
                }
            }

            if (Runtime.invokerKeys != null)
            {
                addressLabel.Text = Runtime.invokerKeys.address;
            }
            else
            {
                addressLabel.Text = "(No key loaded)";
            }

            if (!string.IsNullOrEmpty(MainForm.targetAVMPath))
            {
                var fileName = MainForm.targetAVMPath.Replace(".avm", ".json");
                if (File.Exists(fileName))
                {
                    try
                    {
                        _paramMap = new Dictionary<string, DataNode>();

                        var contents = File.ReadAllText(fileName);

                        var contractInfo = JSONReader.ReadFromString(contents);

                        var contractNode = contractInfo["contract"];
                        var inputs = contractNode["inputs"];

                        paramsList.Items.Clear();
                        foreach (var node in inputs.Children)
                        {
                            var name = node.GetString("name");
                            var data = node.GetNode("params");
                            _paramMap[name] = data;
                        }
                    }
                    finally
                    {
                        
                    }                                    
                }
            }

            button1.Enabled = _paramMap != null && _paramMap.Count > 0 ;
            paramsList.Items.Clear();

            if (_paramMap != null)
            {
                foreach (var entry in _paramMap)
                {
                    paramsList.Items.Add(entry.Key);

                    if (lastParams != null && entry.Key.Equals(lastParams))
                    {
                        paramsList.SelectedIndex = paramsList.Items.Count - 1;
                    }
                }

                if (paramsList.SelectedIndex<0 && paramsList.Items.Count > 0)
                {
                    paramsList.SelectedIndex = 0;
                }

            }

        }

        private void button3_Click(object sender, EventArgs e)
        {
            string input = "";
            if (InputUtils.ShowInputDialog("Enter private key", ref input) == DialogResult.OK)
            {
                var privKey = input.HexToByte();
                if (privKey.Length == 32)
                {
                    Runtime.invokerKeys = new NeoLux.KeyPair(privKey);
                    addressLabel.Text = Runtime.invokerKeys.address;

                    File.WriteAllBytes("last.key", privKey);
                }
                else
                {
                    MessageBox.Show("Invalid private key, length should be 32");
                }
            }
        }

        private void listBox1_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            assetAmmount.Enabled = assetListBox.SelectedIndex > 0;
        }
    }
}
