namespace EcoRouteAI.Views // <-- Doit être identique à MainForm.cs
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            // On appelle aussi le Dispose du ViewModel si nécessaire
            _loadCts?.Dispose();
            _vm?.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code
        // On laisse cette méthode VIDE car tu as ta propre version 
        // dans MainForm.cs qui construit l'interface.
        private void InitializeComponent()
        {
        }
        #endregion
    }
}