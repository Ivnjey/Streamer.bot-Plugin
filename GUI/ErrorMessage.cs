﻿using SuchByte.MacroDeck.GUI.CustomControls;

namespace StreamerbotPlugin.GUI
{
    public partial class ErrorMessage : DialogForm
    {
        public ErrorMessage(string message)
        {
            InitializeComponent();
            label2.Text = message;
        }

        private void btn_OK_Click(object sender, System.EventArgs e)
        {
            Close();
        }
    }
}
