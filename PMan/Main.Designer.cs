namespace PMan {
    partial class Main {
        /// <summary>
        /// Variável de designer necessária.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Limpar os recursos que estão sendo usados.
        /// </summary>
        /// <param name="disposing">true se for necessário descartar os recursos gerenciados; caso contrário, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Código gerado pelo Windows Form Designer

        /// <summary>
        /// Método necessário para suporte ao Designer - não modifique 
        /// o conteúdo deste método com o editor de código.
        /// </summary>
        private void InitializeComponent() {
            this.PluginList = new System.Windows.Forms.ListView();
            this.Plugin = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.Support = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.Type = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.Installed = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.Updated = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.DownloadBNT = new System.Windows.Forms.Button();
            this.UnistallBNT = new System.Windows.Forms.Button();
            this.SearchBNT = new System.Windows.Forms.Button();
            this.SearchTB = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // PluginList
            // 
            this.PluginList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.PluginList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.Plugin,
            this.Support,
            this.Type,
            this.Installed,
            this.Updated});
            this.PluginList.Location = new System.Drawing.Point(12, 12);
            this.PluginList.Name = "PluginList";
            this.PluginList.Size = new System.Drawing.Size(677, 341);
            this.PluginList.TabIndex = 0;
            this.PluginList.UseCompatibleStateImageBehavior = false;
            this.PluginList.View = System.Windows.Forms.View.Details;
            // 
            // Plugin
            // 
            this.Plugin.Text = "Plugin";
            this.Plugin.Width = 217;
            // 
            // Support
            // 
            this.Support.Text = "Supported Files";
            this.Support.Width = 191;
            // 
            // Type
            // 
            this.Type.Text = "Type";
            this.Type.Width = 74;
            // 
            // Installed
            // 
            this.Installed.Text = "Installed";
            this.Installed.Width = 53;
            // 
            // Updated
            // 
            this.Updated.Text = "Updated";
            this.Updated.Width = 61;
            // 
            // DownloadBNT
            // 
            this.DownloadBNT.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.DownloadBNT.Location = new System.Drawing.Point(552, 359);
            this.DownloadBNT.Name = "DownloadBNT";
            this.DownloadBNT.Size = new System.Drawing.Size(137, 23);
            this.DownloadBNT.TabIndex = 1;
            this.DownloadBNT.Text = "Install/Update Selected";
            this.DownloadBNT.UseVisualStyleBackColor = true;
            this.DownloadBNT.Click += new System.EventHandler(this.DownloadBNT_Click);
            // 
            // UnistallBNT
            // 
            this.UnistallBNT.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.UnistallBNT.Location = new System.Drawing.Point(409, 359);
            this.UnistallBNT.Name = "UnistallBNT";
            this.UnistallBNT.Size = new System.Drawing.Size(137, 23);
            this.UnistallBNT.TabIndex = 2;
            this.UnistallBNT.Text = "Unistall Selected";
            this.UnistallBNT.UseVisualStyleBackColor = true;
            this.UnistallBNT.Click += new System.EventHandler(this.UnistallBNT_Click);
            // 
            // SearchBNT
            // 
            this.SearchBNT.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.SearchBNT.Location = new System.Drawing.Point(315, 359);
            this.SearchBNT.Name = "SearchBNT";
            this.SearchBNT.Size = new System.Drawing.Size(61, 23);
            this.SearchBNT.TabIndex = 3;
            this.SearchBNT.Text = "Search";
            this.SearchBNT.UseVisualStyleBackColor = true;
            this.SearchBNT.Click += new System.EventHandler(this.SearchBNT_Click);
            // 
            // SearchTB
            // 
            this.SearchTB.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.SearchTB.Location = new System.Drawing.Point(12, 361);
            this.SearchTB.Name = "SearchTB";
            this.SearchTB.Size = new System.Drawing.Size(297, 20);
            this.SearchTB.TabIndex = 4;
            // 
            // Main
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(701, 394);
            this.Controls.Add(this.SearchTB);
            this.Controls.Add(this.SearchBNT);
            this.Controls.Add(this.UnistallBNT);
            this.Controls.Add(this.DownloadBNT);
            this.Controls.Add(this.PluginList);
            this.Name = "Main";
            this.Text = "Plugin Manager";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListView PluginList;
        private System.Windows.Forms.ColumnHeader Plugin;
        private System.Windows.Forms.ColumnHeader Support;
        private System.Windows.Forms.ColumnHeader Type;
        private System.Windows.Forms.ColumnHeader Updated;
        private System.Windows.Forms.ColumnHeader Installed;
        private System.Windows.Forms.Button DownloadBNT;
        private System.Windows.Forms.Button UnistallBNT;
        private System.Windows.Forms.Button SearchBNT;
        private System.Windows.Forms.TextBox SearchTB;
    }
}

