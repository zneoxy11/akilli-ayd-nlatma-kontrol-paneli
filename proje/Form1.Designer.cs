namespace proje
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            // Initialize components container
            this.components = new System.ComponentModel.Container();

            // Configure form properties
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(550, 800);
            this.Name = "Form1";
            this.Text = "Akıllı Aydınlatma Kontrol Paneli";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;

            // Disable maximize button
            this.MaximizeBox = false;

            // Set form border style
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;

            // If we have any form-level event handlers or additional initialization
            // This method is called in the constructor before CreatePanelsAndControls
            // So you can add any additional setup here if needed
        }

        #endregion
    }
}