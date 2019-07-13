/*
Copyright � Joan Charmant 2008.
joan.charmant@gmail.com 
 
This file is part of Kinovea.

Kinovea is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License version 2 
as published by the Free Software Foundation.

Kinovea is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with Kinovea. If not, see http://www.gnu.org/licenses/.

*/

using System;
using System.ComponentModel;
using System.Reflection;
using System.Resources;
using System.Threading;
using System.Windows.Forms;

namespace Kinovea.ScreenManager
{
    public partial class formFramesExport : Form
    {
        private PlayerScreenUserInterface m_psui;
        private string m_FilePath;
        private Int64 m_iIntervalTimeStamps;
        private bool m_bBlendDrawings;
        private bool m_bKeyframesOnly;
        private bool m_IsIdle = true;
        private ResourceManager m_ResourceManager;
        private int m_iEstimatedTotal;

        public formFramesExport(PlayerScreenUserInterface _psui, string _FilePath, Int64 _iIntervalTimeStamps, bool _bBlendDrawings, bool _bKeyframesOnly, int _iEstimatedTotal)
        {
            InitializeComponent();

            m_psui = _psui;
            m_FilePath = _FilePath;
            m_iIntervalTimeStamps = _iIntervalTimeStamps;
            m_bBlendDrawings = _bBlendDrawings;
            m_bKeyframesOnly = _bKeyframesOnly;
            m_iEstimatedTotal = _iEstimatedTotal;

            m_ResourceManager = new ResourceManager("Kinovea.ScreenManager.Languages.ScreenManagerLang", Assembly.GetExecutingAssembly());
                
            this.Text = "   " + m_ResourceManager.GetString("FormFramesExport_Title", Thread.CurrentThread.CurrentUICulture);
            labelInfos.Text = m_ResourceManager.GetString("FormFramesExport_Infos", Thread.CurrentThread.CurrentUICulture) + " 0 / ~?";

            progressBar.Minimum = 0;
            progressBar.Maximum = 100;
            progressBar.Step = 1;
            progressBar.Style = ProgressBarStyle.Blocks;
            progressBar.Value = 0;

            Application.Idle += new EventHandler(this.IdleDetector);
        }
        private void formFramesExport_Load(object sender, EventArgs e)
        {
            //-----------------------------------
            // Le Handle existe, on peut y aller.
            //-----------------------------------
            DoExport();
        }
        private void IdleDetector(object sender, EventArgs e)
        {
            m_IsIdle = true;
        }
        public void DoExport()
        {
            //--------------------------------------------------
            // Lancer le worker (d�clenche bgWorker_DoWork)
            //--------------------------------------------------
            bgWorker.RunWorkerAsync();
        }
        private void bgWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            //-------------------------------------------------------------
            // /!\ Cette fonction s'execute dans l'espace du WORKER THREAD.
            // Les fonctions appel�es d'ici ne doivent pas toucher l'UI.
            // Les appels ici sont synchrones mais on peut remonter de 
            // l'information par bgWorker_ProgressChanged().
            //-------------------------------------------------------------
            m_psui.SaveImageSequence(bgWorker, m_FilePath, m_iIntervalTimeStamps, m_bBlendDrawings, m_bKeyframesOnly, m_iEstimatedTotal);

            e.Result = 0;
        }
        private void bgWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            //--------------------------------------------------------------------------------
            // Cette fonction s'execute dans le thread local. 
            // On a le droit de mettre � jour l'UI.
            //--------------------------------------------------------------------------------

            //--------------------------------------------------------------------------------
            // Probl�me possible : 
            // Le worker thread va vouloir mettre � jour les donn�es tr�s souvent.
            // Comme le traitement est asynchrone,il se peut qu'il poste des ReportProgress()
            // plus vite qu'ils ne soient trait�s ici.
            // Il faut donc attendre que la form soit idle.
            //--------------------------------------------------------------------------------
            if (m_IsIdle)
            {
                m_IsIdle = false;

                int iTotal = (int)e.UserState;
                int iValue = (int)e.ProgressPercentage;

                if (iValue > iTotal) { iValue = iTotal; }

                progressBar.Maximum = iTotal;
                progressBar.Value = iValue;

                labelInfos.Text = m_ResourceManager.GetString("FormFramesExport_Infos", Thread.CurrentThread.CurrentUICulture) + " " + iValue + " / ~" + iTotal;
            }
        }
        private void bgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //----------------------------------------------------------------------
            // On arrive ici lorsque la fonction bgWorker_DoWork() ressort.
            // Les donn�es dans e doivent �tre mise en place dans bgWorker_DoWork();  
            //----------------------------------------------------------------------

            // Se d�crocher de l'event Idle.
            Application.Idle -= new EventHandler(this.IdleDetector);

            Hide();
        }
    }
}