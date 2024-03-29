#region License
/*
Copyright � Joan Charmant 2008-2009.
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
#endregion

#region Using directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

using AForge.Video.DirectShow;
using Kinovea.ScreenManager.Languages;
using Kinovea.ScreenManager.Properties;
using Kinovea.Services;
using Kinovea.VideoFiles;

#endregion

namespace Kinovea.ScreenManager
{
	public partial class CaptureScreenUserInterface : UserControl, IFrameServerCaptureContainer
	{	
		#region Internal delegates for async methods
		private delegate void InitDecodingSize();
		private InitDecodingSize m_InitDecodingSize;
		#endregion
		
		#region Properties
		#endregion

		#region Members
		private ICaptureScreenUIHandler m_ScreenUIHandler;	// CaptureScreen seen trough a limited interface.
		private FrameServerCapture m_FrameServer;

		// General
		private PreferencesManager m_PrefManager = PreferencesManager.Instance();
		private bool m_bTryingToConnect;

		// Image
		private bool m_bStretchModeOn;			// This is just a toggle to know what to do on double click.
		private bool m_bShowImageBorder;
		private static readonly Pen m_PenImageBorder = Pens.SteelBlue;
		
		// Keyframes, Drawings, etc.
		private DrawingToolType m_ActiveTool;
		private AbstractDrawingTool[] m_DrawingTools;
		private ColorProfile m_ColorProfile = new ColorProfile();
		private bool m_bDocked = true;
		private bool m_bTextEdit;
		private bool m_bMeasuring;
		
		// Other
		private bool m_bSettingsFold;
		private System.Windows.Forms.Timer m_DeselectionTimer = new System.Windows.Forms.Timer();
		
		
		#region Context Menus
		private ContextMenuStrip popMenu = new ContextMenuStrip();
		private ToolStripMenuItem mnuSavePic = new ToolStripMenuItem();
		private ToolStripMenuItem mnuCloseScreen = new ToolStripMenuItem();

		private ContextMenuStrip popMenuDrawings = new ContextMenuStrip();
		private ToolStripMenuItem mnuConfigureDrawing = new ToolStripMenuItem();
		private ToolStripSeparator mnuSepDrawing = new ToolStripSeparator();
		private ToolStripSeparator mnuSepDrawing2 = new ToolStripSeparator();
		private ToolStripMenuItem mnuDeleteDrawing = new ToolStripMenuItem();
		private ToolStripMenuItem mnuShowMeasure = new ToolStripMenuItem();
		private ToolStripMenuItem mnuSealMeasure = new ToolStripMenuItem();
		
		private ContextMenuStrip popMenuMagnifier = new ContextMenuStrip();
		private ToolStripMenuItem mnuMagnifier150 = new ToolStripMenuItem();
		private ToolStripMenuItem mnuMagnifier175 = new ToolStripMenuItem();
		private ToolStripMenuItem mnuMagnifier200 = new ToolStripMenuItem();
		private ToolStripMenuItem mnuMagnifier225 = new ToolStripMenuItem();
		private ToolStripMenuItem mnuMagnifier250 = new ToolStripMenuItem();
		private ToolStripMenuItem mnuMagnifierDirect = new ToolStripMenuItem();
		private ToolStripMenuItem mnuMagnifierQuit = new ToolStripMenuItem();

		private ContextMenuStrip popMenuGrids = new ContextMenuStrip();
		private ToolStripMenuItem mnuGridsConfigure = new ToolStripMenuItem();
		private ToolStripMenuItem mnuGridsHide = new ToolStripMenuItem();
		#endregion

		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		#endregion

		#region Constructor
		public CaptureScreenUserInterface(FrameServerCapture _FrameServer, ICaptureScreenUIHandler _screenUIHandler)
		{
			log.Debug("Constructing the CaptureScreen user interface.");
			m_ScreenUIHandler = _screenUIHandler;
			m_FrameServer = _FrameServer;
			m_FrameServer.SetContainer(this);
			m_FrameServer.Metadata = new Metadata(null, null);
			
			// Initialize UI.
			InitializeComponent();
			this.Dock = DockStyle.Fill;
			ShowHideResizers(false);
			InitializeDrawingTools();
			InitializeMetadata();
			BuildContextMenus();
			m_bDocked = true;
			
			InitializeCaptureFiles();
			
			// Delegates
			m_InitDecodingSize = new InitDecodingSize(InitDecodingSize_Invoked);
			
			m_DeselectionTimer.Interval = 3000;
			m_DeselectionTimer.Tick += new EventHandler(DeselectionTimer_OnTick);

			TryToConnect();
			tmrCaptureDeviceDetector.Start();
		}
		#endregion
		
		#region IFrameServerCaptureContainer implementation
		public void DoInvalidate()
		{
			pbSurfaceScreen.Invalidate();
		}
		public void DoInitDecodingSize()
		{
			BeginInvoke(m_InitDecodingSize);
		}
		private void InitDecodingSize_Invoked()
		{			
			((DrawingToolPointer)m_DrawingTools[(int)DrawingToolType.Pointer]).SetImageSize(m_FrameServer.ImageSize);
			
			m_FrameServer.CoordinateSystem.Stretch = 1;
			m_bStretchModeOn = false;
			
			PanelCenter_Resize(null, EventArgs.Empty);
			
			// As a matter of fact we pass here at the first received frame.
			// We can stop trying to connect now.
			tmrCaptureDeviceDetector.Stop();
			UpdateFilenameLabel();
			OnPoke();
		}
		public void DisplayAsGrabbing(bool _bIsGrabbing)
		{
			if(_bIsGrabbing)
			{
				pbSurfaceScreen.Visible = _bIsGrabbing;
	   			ShowHideResizers(_bIsGrabbing);
				btnGrab.Image = Kinovea.ScreenManager.Properties.Resources.capturepause5;	
			}
			else
			{
				btnGrab.Image = Kinovea.ScreenManager.Properties.Resources.capturegrab5;	
			}
		}
		private void DisplayAsRecording(bool _bIsRecording)
		{
			if(_bIsRecording)
        	{
        		btnRecord.Image = Kinovea.ScreenManager.Properties.Resources.control_recstop;
        		toolTips.SetToolTip(btnRecord, ScreenManagerLang.ToolTip_RecordStop);
        	}
        	else
        	{
				btnRecord.Image = Kinovea.ScreenManager.Properties.Resources.control_rec;        		
				toolTips.SetToolTip(btnRecord, ScreenManagerLang.ToolTip_RecordStart);
        	}
		}
		public void DoUpdateCapturedVideos()
		{
			// Update the list of Captured Videos.
			// Similar to OrganizeKeyframe in PlayerScreen.
			
			pnlThumbnails.Controls.Clear();
			
			if(m_FrameServer.RecentlyCapturedVideos.Count > 0)
			{
				int iBoxIndex = 0;
				int iPixelsOffset = 0;
				int iPixelsSpacing = 20;
				
				foreach (CapturedVideo cv in m_FrameServer.RecentlyCapturedVideos)
				{
					CapturedVideoBox box = new CapturedVideoBox(cv);
					SetupDefaultThumbBox(box);
					
					// Finish the setup
					box.Left = iPixelsOffset + iPixelsSpacing;
					box.pbThumbnail.Image = cv.Thumbnail;
					//box.CloseThumb += new KeyframeBox.CloseThumbHandler(ThumbBoxClose);
					//box.ClickThumb += new KeyframeBox.ClickThumbHandler(ThumbBoxClick);
					
					iPixelsOffset += (iPixelsSpacing + box.Width);

					pnlThumbnails.Controls.Add(box);

					iBoxIndex++;
				}
				
				DockKeyframePanel(false);
				pnlThumbnails.Refresh();
			}
			else
			{
				DockKeyframePanel(true);
			}

		}
		#endregion
		
		#region Public Methods
		public void DisplayAsActiveScreen(bool _bActive)
		{
			// Called from ScreenManager.
			ShowBorder(_bActive);
		}
		public void RefreshUICulture()
		{
			// Labels
			lblSettings.Text = "   " + ScreenManagerLang.Generic_Configuration;
			lblImageFile.Text = ScreenManagerLang.Generic_Image;
			lblSpeedTuner.Text = ScreenManagerLang.Generic_Video;
			
			ReloadTooltipsCulture();
			ReloadMenusCulture();
			
			// Refresh image to update grids colors, etc.
			pbSurfaceScreen.Invalidate();
		}
		public bool OnKeyPress(Keys _keycode)
		{
			bool bWasHandled = false;
			
			// Method called from the Screen Manager's PreFilterMessage.
			switch (_keycode)
			{
				case Keys.Escape:
					{
						DisablePlayAndDraw();
						pbSurfaceScreen.Invalidate();
						bWasHandled = true;
						break;
					}
				
				case Keys.Add:
					{
						IncreaseDirectZoom();
						bWasHandled = true;
						break;
					}
				case Keys.Subtract:
					{
						// Decrease Zoom.
						DecreaseDirectZoom();
						bWasHandled = true;
						break;
					}
				case Keys.F11:
					{
						ToggleStretchMode();
						bWasHandled = true;
						break;
					}
				case Keys.Delete:
					{
						// Remove selected Drawing
						// Note: Should only work if the Drawing is currently being moved...
						DeleteSelectedDrawing();
						
						bWasHandled = true;
						break;
					}
				default:
					break;
			}

			return bWasHandled;
		}
		public void AddSVGDrawing(string _filename)
		{
			// Add an SVG drawing from the file.
			// Mimick all the actions that are normally taken when we select a drawing tool and click on the image.
			
			if(m_FrameServer.IsConnected && File.Exists(_filename))
			{		
				m_FrameServer.Metadata.AllDrawingTextToNormalMode();
				
				try
				{
					Point imageCenter = new Point(m_FrameServer.Metadata.ImageSize.Width / 2, m_FrameServer.Metadata.ImageSize.Height / 2);
					DrawingSVG dsvg = new DrawingSVG(imageCenter.X,
					                                 imageCenter.Y, 
					                                 0, 
					                                 m_FrameServer.Metadata.AverageTimeStampsPerFrame, 
					                                 _filename);
									
					m_FrameServer.Metadata[0].AddDrawing(dsvg);
					m_FrameServer.Metadata.SelectedDrawingFrame = 0;
					m_FrameServer.Metadata.SelectedDrawing = 0;
				}
				catch
				{
					// An error occurred during the creation.
					// example : external DTD an no network or invalid svg file.
					log.Error("An error occurred during the creation of an SVG drawing.");
				}
				
				pbSurfaceScreen.Invalidate();
			}
		}
		public void BeforeClose()
		{
			// This screen is about to be closed.
			tmrCaptureDeviceDetector.Stop();
			tmrCaptureDeviceDetector.Dispose();
			PreferencesManager.Instance().Export();
		}
		#endregion
		
		#region Various Inits & Setups
		private void InitializeDrawingTools()
        {
			// Create Drawing Tools
			m_DrawingTools = new AbstractDrawingTool[(int)DrawingToolType.NumberOfDrawingTools];
			
			m_DrawingTools[(int)DrawingToolType.Pointer] = new DrawingToolPointer();
			m_DrawingTools[(int)DrawingToolType.Line2D] = new DrawingToolLine2D();
			m_DrawingTools[(int)DrawingToolType.Cross2D] = new DrawingToolCross2D();
			m_DrawingTools[(int)DrawingToolType.Angle2D] = new DrawingToolAngle2D();
			m_DrawingTools[(int)DrawingToolType.Pencil] = new DrawingToolPencil();
			m_DrawingTools[(int)DrawingToolType.Text] = new DrawingToolText();
			//m_DrawingTools[(int)DrawingToolType.Chrono] = new DrawingToolChrono();
			
			m_ActiveTool = DrawingToolType.Pointer;
        }
		private void InitializeMetadata()
		{
			// In capture, there is always a single keyframe.
			// All drawings are considered motion guides.
			Keyframe kf = new Keyframe(m_FrameServer.Metadata);
			kf.Position = 0;
			m_FrameServer.Metadata.Add(kf);
		}
		private void ShowHideResizers(bool _bShow)
		{
			ImageResizerNE.Visible = _bShow;
			ImageResizerNW.Visible = _bShow;
			ImageResizerSE.Visible = _bShow;
			ImageResizerSW.Visible = _bShow;
		}
		private void BuildContextMenus()
		{
			// Attach the event handlers and build the menus.
			
			// 1. Default context menu.
			mnuSavePic.Click += new EventHandler(btnSnapShot_Click);
			mnuSavePic.Image = Properties.Resources.picture_save;
			mnuCloseScreen.Click += new EventHandler(btnClose_Click);
			mnuCloseScreen.Image = Properties.Resources.capture_close2;
			popMenu.Items.AddRange(new ToolStripItem[] { mnuSavePic, new ToolStripSeparator(), mnuCloseScreen });

			// 2. Drawings context menu (Configure, Delete, Track this)
			mnuConfigureDrawing.Click += new EventHandler(mnuConfigureDrawing_Click);
			mnuConfigureDrawing.Image = Properties.Resources.wrench;
			mnuDeleteDrawing.Click += new EventHandler(mnuDeleteDrawing_Click);
			mnuDeleteDrawing.Image = Properties.Resources.delete;
			mnuShowMeasure.Click += new EventHandler(mnuShowMeasure_Click);
			mnuShowMeasure.Image = Properties.Resources.measure;
			mnuSealMeasure.Click += new EventHandler(mnuSealMeasure_Click);
			mnuSealMeasure.Image = Properties.Resources.textfield;
			popMenuDrawings.Items.AddRange(new ToolStripItem[] { mnuConfigureDrawing, mnuSepDrawing, mnuShowMeasure, mnuSealMeasure, mnuSepDrawing2, mnuDeleteDrawing });

			// 5. Magnifier
			mnuMagnifier150.Click += new EventHandler(mnuMagnifier150_Click);
			mnuMagnifier175.Click += new EventHandler(mnuMagnifier175_Click);
			mnuMagnifier175.Checked = true;
			mnuMagnifier200.Click += new EventHandler(mnuMagnifier200_Click);
			mnuMagnifier225.Click += new EventHandler(mnuMagnifier225_Click);
			mnuMagnifier250.Click += new EventHandler(mnuMagnifier250_Click);
			mnuMagnifierDirect.Click += new EventHandler(mnuMagnifierDirect_Click);
			mnuMagnifierDirect.Image = Properties.Resources.arrow_out;
			mnuMagnifierQuit.Click += new EventHandler(mnuMagnifierQuit_Click);
			mnuMagnifierQuit.Image = Properties.Resources.hide2;
			popMenuMagnifier.Items.AddRange(new ToolStripItem[] { mnuMagnifier150, mnuMagnifier175, mnuMagnifier200, mnuMagnifier225, mnuMagnifier250, new ToolStripSeparator(), mnuMagnifierDirect, mnuMagnifierQuit });
			
			// 6. Grids
			mnuGridsConfigure.Click += new EventHandler(mnuGridsConfigure_Click);
			mnuGridsConfigure.Image = Properties.Resources.wrench;
			mnuGridsHide.Click += new EventHandler(mnuGridsHide_Click);
			mnuGridsHide.Image = Properties.Resources.hide2;
			popMenuGrids.Items.AddRange(new ToolStripItem[] { mnuGridsConfigure, mnuGridsHide });
			
			// Default :
			//this.ContextMenuStrip = popMenu;
			
			// The right context menu and its content will be choosen upon MouseDown.
			panelCenter.ContextMenuStrip = popMenu;
			
			// Load texts
			ReloadMenusCulture();
		}
		private void InitializeCaptureFiles()
		{
			tbImageFilename.Text = CreateNewFilename(null);
			tbVideoFilename.Text = tbImageFilename.Text;
			
			PreferencesManager pm = PreferencesManager.Instance();
			tbImageDirectory.Text = pm.CaptureImageDirectory;
			tbVideoDirectory.Text = pm.CaptureVideoDirectory;
		}
		private void UpdateFilenameLabel()
		{
			lblFileName.Text = m_FrameServer.DeviceName;
		}
		#endregion
		
		#region Misc Events
		private void btnClose_Click(object sender, EventArgs e)
		{
			// Propagate to PlayerScreen which will report to ScreenManager.
			m_ScreenUIHandler.ScreenUI_CloseAsked();
		}
		private void DeselectionTimer_OnTick(object sender, EventArgs e) 
		{
			// Deselect the currently selected drawing.
			// This is used for drawings that must show extra stuff for being transformed, but we 
			// don't want to show the extra stuff all the time for clarity.
			
			m_FrameServer.Metadata.SelectedDrawingFrame = -1;
			m_FrameServer.Metadata.SelectedDrawing = -1;
			log.Debug("Deselection timer fired.");
			m_DeselectionTimer.Stop();
			pbSurfaceScreen.Invalidate();
		}
		#endregion
		
		#region Misc private helpers
		private void OnPoke()
		{
			//------------------------------------------------------------------------------
			// This function is a hub event handler for all button press, mouse clicks, etc.
			// Signal itself as the active screen to the ScreenManager
			//---------------------------------------------------------------------
			
			m_ScreenUIHandler.ScreenUI_SetAsActiveScreen();
			
			// 1. Ensure no DrawingText is in edit mode.
			m_FrameServer.Metadata.AllDrawingTextToNormalMode();

			// 2. Return to the pointer tool, except if Pencil
			if (m_ActiveTool != DrawingToolType.Pencil)
			{
				m_ActiveTool = DrawingToolType.Pointer;
				SetCursor(m_DrawingTools[(int)m_ActiveTool].GetCursor(Color.Empty, -1));
			}

			// 3. Dock Keyf panel if nothing to see.
			if (m_FrameServer.RecentlyCapturedVideos.Count < 1)
			{
				DockKeyframePanel(true);
			}
		}
		private void DoDrawingUndrawn()
		{
			//--------------------------------------------------------
			// this function is called after we undo a drawing action.
			// Called from CommandAddDrawing.Unexecute() through a delegate.
			//--------------------------------------------------------

			// Return to the pointer tool unless we were drawing.
			if (m_ActiveTool != DrawingToolType.Pencil)
			{
				m_ActiveTool = DrawingToolType.Pointer;
				SetCursor(m_DrawingTools[(int)m_ActiveTool].GetCursor(Color.Empty, 0));
			}
		}
		private void ShowBorder(bool _bShow)
		{
			m_bShowImageBorder = _bShow;
			pbSurfaceScreen.Invalidate();
		}
		private void DrawImageBorder(Graphics _canvas)
		{
			// Draw the border around the screen to mark it as selected.
			// Called back from main drawing routine.
			_canvas.DrawRectangle(m_PenImageBorder, 0, 0, pbSurfaceScreen.Width - m_PenImageBorder.Width, pbSurfaceScreen.Height - m_PenImageBorder.Width);
		}
		private void DisablePlayAndDraw()
		{
			m_ActiveTool = DrawingToolType.Pointer;
			SetCursor(m_DrawingTools[(int)m_ActiveTool].GetCursor(Color.Empty, 0));
			DisableMagnifier();
			UnzoomDirectZoom();
		}
		#endregion
		
		#region Video Controls
		private void btnGrab_Click(object sender, EventArgs e)
		{
			if(m_FrameServer.IsConnected)
			{
				if(m_FrameServer.IsGrabbing)
				{
					m_FrameServer.PauseGrabbing();
				}
			   	else
			   	{
					m_FrameServer.StartGrabbing();
			   	}	
				
			   	OnPoke();	
			}
		}
		public void Common_MouseWheel(object sender, MouseEventArgs e)
		{
			// MouseWheel was recorded on one of the controls.
			if(m_FrameServer.IsConnected)
			{
				int iScrollOffset = e.Delta * SystemInformation.MouseWheelScrollLines / 120;

				if ((ModifierKeys & Keys.Control) == Keys.Control)
				{
					if (iScrollOffset > 0)
					{
						IncreaseDirectZoom();
					}
					else
					{
						DecreaseDirectZoom();
					}
				}
				else
				{
					// return in recent frame history ?	
				}
			}
			
		}
		#endregion

		#region Auto Stretch & Manual Resize
		private void StretchSqueezeSurface()
		{
			if (m_FrameServer.IsGrabbing)
			{
				// Check if the image was loaded squeezed.
				// (happen when screen control isn't being fully expanded at video load time.)
				if(pbSurfaceScreen.Height < panelCenter.Height && m_FrameServer.CoordinateSystem.Stretch < 1.0)
				{
					m_FrameServer.CoordinateSystem.Stretch = 1.0;
				}
				
				Size imgSize = m_FrameServer.ImageSize;
				
				//---------------------------------------------------------------
				// Check if the stretch factor is not going to outsize the panel.
				// If so, force maximized, unless screen is smaller than video.
				//---------------------------------------------------------------
				int iTargetHeight = (int)((double)imgSize.Height * m_FrameServer.CoordinateSystem.Stretch);
				int iTargetWidth = (int)((double)imgSize.Width * m_FrameServer.CoordinateSystem.Stretch);
				
				if (iTargetHeight > panelCenter.Height || iTargetWidth > panelCenter.Width)
				{
					if (m_FrameServer.CoordinateSystem.Stretch > 1.0)
					{
						m_bStretchModeOn = true;
					}
				}
				
				if ((m_bStretchModeOn) || (imgSize.Width > panelCenter.Width) || (imgSize.Height > panelCenter.Height))
				{
					//-------------------------------------------------------------------------------
					// Maximiser :
					// Redimensionner l'image selon la dimension la plus proche de la taille du panel.
					//-------------------------------------------------------------------------------
					float WidthRatio = (float)imgSize.Width / panelCenter.Width;
					float HeightRatio = (float)imgSize.Height / panelCenter.Height;
					
					if (WidthRatio > HeightRatio)
					{
						pbSurfaceScreen.Width = panelCenter.Width;
						pbSurfaceScreen.Height = (int)((float)imgSize.Height / WidthRatio);
						
						m_FrameServer.CoordinateSystem.Stretch = (1 / WidthRatio);
					}
					else
					{
						pbSurfaceScreen.Width = (int)((float)imgSize.Width / HeightRatio);
						pbSurfaceScreen.Height = panelCenter.Height;
						
						m_FrameServer.CoordinateSystem.Stretch = (1 / HeightRatio);
					}
				}
				else
				{
					pbSurfaceScreen.Width = (int)((double)imgSize.Width * m_FrameServer.CoordinateSystem.Stretch);
					pbSurfaceScreen.Height = (int)((double)imgSize.Height * m_FrameServer.CoordinateSystem.Stretch);
				}
				
				// Center
				pbSurfaceScreen.Left = (panelCenter.Width / 2) - (pbSurfaceScreen.Width / 2);
				pbSurfaceScreen.Top = (panelCenter.Height / 2) - (pbSurfaceScreen.Height / 2);
				ReplaceResizers();
				
				// Redefine grids.
				Size imageSize = new Size(imgSize.Width, imgSize.Height);
				m_FrameServer.Metadata.Plane.SetLocations(imageSize, m_FrameServer.CoordinateSystem.Stretch, m_FrameServer.CoordinateSystem.Location);
				m_FrameServer.Metadata.Grid.SetLocations(imageSize, m_FrameServer.CoordinateSystem.Stretch, m_FrameServer.CoordinateSystem.Location);	
			}
		}
		private void ReplaceResizers()
		{
			ImageResizerSE.Left = pbSurfaceScreen.Left + pbSurfaceScreen.Width - (ImageResizerSE.Width / 2);
			ImageResizerSE.Top = pbSurfaceScreen.Top + pbSurfaceScreen.Height - (ImageResizerSE.Height / 2);

			ImageResizerSW.Left = pbSurfaceScreen.Left - (ImageResizerSW.Width / 2);
			ImageResizerSW.Top = pbSurfaceScreen.Top + pbSurfaceScreen.Height - (ImageResizerSW.Height / 2);

			ImageResizerNE.Left = pbSurfaceScreen.Left + pbSurfaceScreen.Width - (ImageResizerNE.Width / 2);
			ImageResizerNE.Top = pbSurfaceScreen.Top - (ImageResizerNE.Height / 2);

			ImageResizerNW.Left = pbSurfaceScreen.Left - (ImageResizerNW.Width / 2);
			ImageResizerNW.Top = pbSurfaceScreen.Top - (ImageResizerNW.Height / 2);
		}
		private void ToggleStretchMode()
		{
			if (!m_bStretchModeOn)
			{
				m_bStretchModeOn = true;
			}
			else
			{
				// Ne pas repasser en stretch mode � false si on est plus petit que l'image
				if (m_FrameServer.CoordinateSystem.Stretch >= 1)
				{
					m_FrameServer.CoordinateSystem.Stretch = 1;
					m_bStretchModeOn = false;
				}
			}
			StretchSqueezeSurface();
			m_FrameServer.Metadata.ResizeFinished();
			pbSurfaceScreen.Invalidate();
		}
		private void ImageResizerSE_MouseMove(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				int iTargetHeight = (ImageResizerSE.Top - pbSurfaceScreen.Top + e.Y);
				int iTargetWidth = (ImageResizerSE.Left - pbSurfaceScreen.Left + e.X);
				ResizeImage(iTargetWidth, iTargetHeight);
			}
		}
		private void ImageResizerSW_MouseMove(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				int iTargetHeight = (ImageResizerSW.Top - pbSurfaceScreen.Top + e.Y);
				int iTargetWidth = pbSurfaceScreen.Width + (pbSurfaceScreen.Left - (ImageResizerSW.Left + e.X));
				ResizeImage(iTargetWidth, iTargetHeight);
			}
		}
		private void ImageResizerNW_MouseMove(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				int iTargetHeight = pbSurfaceScreen.Height + (pbSurfaceScreen.Top - (ImageResizerNW.Top + e.Y));
				int iTargetWidth = pbSurfaceScreen.Width + (pbSurfaceScreen.Left - (ImageResizerNW.Left + e.X));
				ResizeImage(iTargetWidth, iTargetHeight);
			}
		}
		private void ImageResizerNE_MouseMove(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				int iTargetHeight = pbSurfaceScreen.Height + (pbSurfaceScreen.Top - (ImageResizerNE.Top + e.Y));
				int iTargetWidth = (ImageResizerNE.Left - pbSurfaceScreen.Left + e.X);
				ResizeImage(iTargetWidth, iTargetHeight);
			}
		}
		private void ResizeImage(int _iTargetWidth, int _iTargetHeight)
		{
			//-------------------------------------------------------------------
			// Resize at the following condition:
			// Bigger than original image size, smaller than panel size.
			//-------------------------------------------------------------------
			if (_iTargetHeight > m_FrameServer.ImageSize.Height &&
			    _iTargetHeight < panelCenter.Height &&
			    _iTargetWidth > m_FrameServer.ImageSize.Width &&
			    _iTargetWidth < panelCenter.Width)
			{
				double fHeightFactor = ((_iTargetHeight) / (double)m_FrameServer.ImageSize.Height);
				double fWidthFactor = ((_iTargetWidth) / (double)m_FrameServer.ImageSize.Width);

				m_FrameServer.CoordinateSystem.Stretch = (fWidthFactor + fHeightFactor) / 2;
				m_bStretchModeOn = false;
				StretchSqueezeSurface();
				pbSurfaceScreen.Invalidate();
			}
		}
		private void Resizers_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			ToggleStretchMode();
		}
		private void Resizers_MouseUp(object sender, MouseEventArgs e)
		{
			m_FrameServer.Metadata.ResizeFinished();
			pbSurfaceScreen.Invalidate();
		}
		#endregion
		
		#region Culture
		private void ReloadMenusCulture()
		{
			// Reload the text for each menu.
			// this is done at construction time and at RefreshUICulture time.
			
			// 1. Default context menu.
			mnuSavePic.Text = ScreenManagerLang.Generic_SaveImage;
			mnuCloseScreen.Text = ScreenManagerLang.mnuCloseScreen;
			
			// 2. Drawings context menu.
			mnuConfigureDrawing.Text = ScreenManagerLang.mnuConfigureDrawing_ColorSize;
			mnuDeleteDrawing.Text = ScreenManagerLang.mnuDeleteDrawing;
			mnuShowMeasure.Text = ScreenManagerLang.mnuShowMeasure;
			mnuSealMeasure.Text = ScreenManagerLang.mnuSealMeasure;
			
			// 5. Magnifier
			mnuMagnifier150.Text = ScreenManagerLang.mnuMagnifier150;
			mnuMagnifier175.Text = ScreenManagerLang.mnuMagnifier175;
			mnuMagnifier200.Text = ScreenManagerLang.mnuMagnifier200;
			mnuMagnifier225.Text = ScreenManagerLang.mnuMagnifier225;
			mnuMagnifier250.Text = ScreenManagerLang.mnuMagnifier250;
			mnuMagnifierDirect.Text = ScreenManagerLang.mnuMagnifierDirect;	
			mnuMagnifierQuit.Text = ScreenManagerLang.mnuMagnifierQuit;
				
			// 6. Grids
			mnuGridsConfigure.Text = ScreenManagerLang.mnuConfigureDrawing_ColorSize;	
			mnuGridsHide.Text = ScreenManagerLang.mnuGridsHide;
		}
		private void ReloadTooltipsCulture()
		{
			toolTips.SetToolTip(btnBrowseImageLocation, ScreenManagerLang.ToolTip_SelectFolder);
			toolTips.SetToolTip(btnBrowseVideoLocation, ScreenManagerLang.ToolTip_SelectFolder);
			toolTips.SetToolTip(btnSaveImageLocation, ScreenManagerLang.ToolTip_SelectFolder);
			toolTips.SetToolTip(btnSaveVideoLocation, ScreenManagerLang.ToolTip_SelectFolder);
			
			// Video controls
			toolTips.SetToolTip(btnGrab, ScreenManagerLang.ToolTip_Play);
			toolTips.SetToolTip(btnCamSnap, ScreenManagerLang.Generic_SaveImage);
			toolTips.SetToolTip(btnCamSettings, ScreenManagerLang.ToolTip_DevicePicker);
			toolTips.SetToolTip(btnRecord, m_FrameServer.IsRecording ? ScreenManagerLang.ToolTip_RecordStop : ScreenManagerLang.ToolTip_RecordStart);

			// Drawing tools
			toolTips.SetToolTip(btnDrawingToolPointer, ScreenManagerLang.ToolTip_DrawingToolPointer);
			toolTips.SetToolTip(btnDrawingToolText, ScreenManagerLang.ToolTip_DrawingToolText);
			toolTips.SetToolTip(btnDrawingToolPencil, ScreenManagerLang.ToolTip_DrawingToolPencil);
			toolTips.SetToolTip(btnDrawingToolLine2D, ScreenManagerLang.ToolTip_DrawingToolLine2D);
			toolTips.SetToolTip(btnDrawingToolCross2D, ScreenManagerLang.ToolTip_DrawingToolCross2D);
			toolTips.SetToolTip(btnDrawingToolAngle2D, ScreenManagerLang.ToolTip_DrawingToolAngle2D);
			toolTips.SetToolTip(btnColorProfile, ScreenManagerLang.ToolTip_ColorProfile);
			toolTips.SetToolTip(btnMagnifier, ScreenManagerLang.ToolTip_Magnifier);
			toolTips.SetToolTip(btn3dplane, ScreenManagerLang.mnu3DPlane);

		}
		private void SetPopupConfigureParams(AbstractDrawing _drawing)
		{
			// choose between "Color" and "Color & Size" popup menu.

			if (_drawing is DrawingAngle2D || _drawing is DrawingCross2D)
			{
				mnuConfigureDrawing.Text = ScreenManagerLang.mnuConfigureDrawing_Color;
			}
			else
			{
				mnuConfigureDrawing.Text = ScreenManagerLang.mnuConfigureDrawing_ColorSize;
			}
			
			// Check Show Measure menu
			if(_drawing is DrawingLine2D)
			{
				mnuShowMeasure.Checked = ((DrawingLine2D)_drawing).ShowMeasure;
			}
		}
		#endregion

		#region SurfaceScreen Events
		private void SurfaceScreen_MouseDown(object sender, MouseEventArgs e)
		{
			if(m_FrameServer.IsConnected)
			{
				m_DeselectionTimer.Stop();
				
				if (e.Button == MouseButtons.Left)
				{
					if (m_FrameServer.IsConnected)
					{
						if ( (m_ActiveTool == DrawingToolType.Pointer)      &&
						    (m_FrameServer.Magnifier.Mode != MagnifierMode.NotVisible) &&
						    (m_FrameServer.Magnifier.IsOnObject(e)))
						{
							m_FrameServer.Magnifier.OnMouseDown(e);
						}
						else
						{
							//-------------------------------------
							// Action begins:
							// Move or set magnifier
							// Move or set Drawing
							// Move Grids
							//-------------------------------------
						
							Point descaledMouse = m_FrameServer.CoordinateSystem.Untransform(e.Location);
						
							// 1. Pass all DrawingText to normal mode
							m_FrameServer.Metadata.AllDrawingTextToNormalMode();
						
							if (m_ActiveTool == DrawingToolType.Pointer)
							{
								// 1. Manipulating an object or Magnifier
								bool bMovingMagnifier = false;
								bool bDrawingHit = false;
							
								// Show the grabbing hand cursor.
								SetCursor(m_DrawingTools[(int)m_ActiveTool].GetCursor(Color.Empty, 1));
							
								if (m_FrameServer.Magnifier.Mode == MagnifierMode.Indirect)
								{
									bMovingMagnifier = m_FrameServer.Magnifier.OnMouseDown(e);
								}
							
								if (!bMovingMagnifier)
								{
									// Magnifier wasn't hit or is not in use,
									// try drawings (including chronos, grids, etc.)
									bDrawingHit = ((DrawingToolPointer)m_DrawingTools[(int)m_ActiveTool]).OnMouseDown(m_FrameServer.Metadata, 0, descaledMouse, 0, m_PrefManager.DefaultFading.Enabled);
								}
							}
							else
							{
								//-----------------------
								// Creating a new Drawing
								//-----------------------
								if (m_ActiveTool != DrawingToolType.Text)
								{
									// Add an instance of a drawing from the active tool to the current keyframe.
									// The drawing is initialized with the current mouse coordinates.
									AbstractDrawing ad = m_DrawingTools[(int)m_ActiveTool].GetNewDrawing(descaledMouse, 0, 1);
									
									m_FrameServer.Metadata[0].AddDrawing(ad);
									m_FrameServer.Metadata.SelectedDrawingFrame = 0;
									m_FrameServer.Metadata.SelectedDrawing = 0;
									
									// Color
									m_ColorProfile.SetupDrawing(ad, m_ActiveTool);
									
									// Special preparation if it's a line.
									DrawingLine2D line = ad as DrawingLine2D;
									if(line != null)
									{
										line.ParentMetadata = m_FrameServer.Metadata;
										line.ShowMeasure = m_bMeasuring;
									}
								}
								else
								{
									
									// We are using the Text Tool. This is a special case because
									// if we are on an existing Textbox, we just go into edit mode
									// otherwise, we add and setup a new textbox.
									bool bEdit = false;
									foreach (AbstractDrawing ad in m_FrameServer.Metadata[0].Drawings)
									{
										if (ad is DrawingText)
										{
											int hitRes = ad.HitTest(descaledMouse, 0);
											if (hitRes >= 0)
											{
												bEdit = true;
												((DrawingText)ad).EditMode = true;
											}
										}
									}
									
									// If we are not on an existing textbox : create new DrawingText.
									if (!bEdit)
									{
										m_FrameServer.Metadata[0].AddDrawing(m_DrawingTools[(int)m_ActiveTool].GetNewDrawing(descaledMouse, 0, 1));
										m_FrameServer.Metadata.SelectedDrawingFrame = 0;
										m_FrameServer.Metadata.SelectedDrawing = 0;
										
										DrawingText dt = (DrawingText)m_FrameServer.Metadata[0].Drawings[0];
										
										dt.ContainerScreen = pbSurfaceScreen;
										dt.RelocateEditbox(m_FrameServer.CoordinateSystem.Stretch * m_FrameServer.CoordinateSystem.Zoom, m_FrameServer.CoordinateSystem.Location);
										dt.EditMode = true;
										panelCenter.Controls.Add(dt.EditBox);
										dt.EditBox.BringToFront();
										dt.EditBox.Focus();
										m_ColorProfile.SetupDrawing(dt, DrawingToolType.Text);
									}
								}
							}
						}	
					}
				}
				else if (e.Button == MouseButtons.Right)
				{
					// Show the right Pop Menu depending on context.
					// (Drawing, Grids, Magnifier, Nothing)
					
					Point descaledMouse = m_FrameServer.CoordinateSystem.Untransform(e.Location);
					
					if (m_FrameServer.IsConnected)
					{
						m_FrameServer.Metadata.UnselectAll();
						
						if (m_FrameServer.Metadata.IsOnDrawing(0, descaledMouse, 0))
						{
							// If we are on a Cross2D, we activate the menu to let the user Track it.
							AbstractDrawing ad = m_FrameServer.Metadata.Keyframes[0].Drawings[m_FrameServer.Metadata.SelectedDrawing];
							
							// We use temp variables because ToolStripMenuItem.Visible always returns false...
							bool isLine = (ad is DrawingLine2D);
							bool configVisible = !(ad is DrawingSVG);
							
							mnuConfigureDrawing.Visible = configVisible;
							mnuShowMeasure.Visible = isLine;
							mnuSealMeasure.Visible = isLine;
							
							mnuSepDrawing.Visible = !(ad is DrawingSVG);
							mnuSepDrawing2.Visible = isLine;
							
							
							// "Color & Size" or "Color" depending on drawing type.
							SetPopupConfigureParams(ad);
							
							panelCenter.ContextMenuStrip = popMenuDrawings;
						}
						else if (m_FrameServer.Metadata.IsOnGrid(descaledMouse))
						{
							panelCenter.ContextMenuStrip = popMenuGrids;
						}
						else if (m_FrameServer.Magnifier.Mode == MagnifierMode.Indirect && m_FrameServer.Magnifier.IsOnObject(e))
						{
							panelCenter.ContextMenuStrip = popMenuMagnifier;
						}
						else if(m_ActiveTool != DrawingToolType.Pointer)
						{
							// Launch Preconfigure dialog.
							// = Updates the tool's entry of the main color profile.
							formConfigureDrawing fcd = new formConfigureDrawing(m_ActiveTool, m_ColorProfile);
							LocateForm(fcd);
							fcd.ShowDialog();
							fcd.Dispose();
							
							UpdateCursor();
						}
						else
						{
							// No drawing touched and no tool selected
							panelCenter.ContextMenuStrip = popMenu;
						}
					}
				}
					
				pbSurfaceScreen.Invalidate();
			}
		}
		private void SurfaceScreen_MouseMove(object sender, MouseEventArgs e)
		{
			// We must keep the same Z order.
			// 1:Magnifier, 2:Drawings, 3:Chronos/Tracks, 4:Grids.
			// When creating a drawing, the active tool will stay on this drawing until its setup is over.
			// After the drawing is created, we either fall back to Pointer tool or stay on the same tool.

			if(m_FrameServer.IsConnected)
			{
				if (e.Button == MouseButtons.None && m_FrameServer.Magnifier.Mode == MagnifierMode.Direct)
				{
					m_FrameServer.Magnifier.MouseX = e.X;
					m_FrameServer.Magnifier.MouseY = e.Y;
					pbSurfaceScreen.Invalidate();
				}
				else if (e.Button == MouseButtons.Left)
				{
					if (m_ActiveTool != DrawingToolType.Pointer)
					{
						// Currently setting the second point of a Drawing.
						m_DrawingTools[(int)m_ActiveTool].OnMouseMove(m_FrameServer.Metadata[0], m_FrameServer.CoordinateSystem.Untransform(new Point(e.X, e.Y)));
					}
					else
					{
						bool bMovingMagnifier = false;
						if (m_FrameServer.Magnifier.Mode == MagnifierMode.Indirect)
						{
							bMovingMagnifier = m_FrameServer.Magnifier.OnMouseMove(e);
						}
						
						if (!bMovingMagnifier && m_ActiveTool == DrawingToolType.Pointer)
						{
							// Moving an object.
							
							Point descaledMouse = m_FrameServer.CoordinateSystem.Untransform(e.Location);
							
							// Magnifier is not being moved or is invisible, try drawings through pointer tool.
							bool bMovingObject = ((DrawingToolPointer)m_DrawingTools[(int)m_ActiveTool]).OnMouseMove(m_FrameServer.Metadata, 0, descaledMouse, m_FrameServer.CoordinateSystem.Location, ModifierKeys);
							
							if (!bMovingObject && m_FrameServer.CoordinateSystem.Zooming)
							{
								// User is not moving anything and we are zooming : move the zoom window.
								
								// Get mouse deltas (descaled=in image coords).
								double fDeltaX = (double)((DrawingToolPointer)m_DrawingTools[(int)m_ActiveTool]).MouseDelta.X;
								double fDeltaY = (double)((DrawingToolPointer)m_DrawingTools[(int)m_ActiveTool]).MouseDelta.Y;
								
								m_FrameServer.CoordinateSystem.MoveZoomWindow(fDeltaX, fDeltaY);
							}
						}
					}
				}
					
				/*if (!m_bIsCurrentlyPlaying)
				{
					pbSurfaceScreen.Invalidate();
				}*/
			}
		}
		private void SurfaceScreen_MouseUp(object sender, MouseEventArgs e)
		{
			// End of an action.
			// Depending on the active tool we have various things to do.
			
			if(m_FrameServer.IsConnected && e.Button == MouseButtons.Left)
			{
				if (m_ActiveTool == DrawingToolType.Pointer)
				{
					OnPoke();
				}
				
				m_FrameServer.Magnifier.OnMouseUp(e);
				
				// Memorize the action we just finished to enable undo.
				if (m_ActiveTool != DrawingToolType.Pointer)
				{
					// Record the adding unless we are editing a text box.
					if (!m_bTextEdit)
					{
						IUndoableCommand cad = new CommandAddDrawing(DoInvalidate, DoDrawingUndrawn, m_FrameServer.Metadata, m_FrameServer.Metadata[0].Position);
						CommandManager cm = CommandManager.Instance();
						cm.LaunchUndoableCommand(cad);
						
					}
					else
					{
						m_bTextEdit = false;
					}
				}
				
				// The fact that we stay on this tool or fall back to pointer tool, depends on the tool.
				m_ActiveTool = m_DrawingTools[(int)m_ActiveTool].OnMouseUp();
				
				if (m_ActiveTool == DrawingToolType.Pointer)
				{
					SetCursor(m_DrawingTools[(int)DrawingToolType.Pointer].GetCursor(Color.Empty, 0));
					((DrawingToolPointer)m_DrawingTools[(int)DrawingToolType.Pointer]).OnMouseUp();
				
					// If we were resizing an SVG drawing, trigger a render.
					// TODO: this is currently triggered on every mouse up, not only on resize !
					int selectedFrame = m_FrameServer.Metadata.SelectedDrawingFrame;
					int selectedDrawing = m_FrameServer.Metadata.SelectedDrawing;
					if(selectedFrame != -1 && selectedDrawing  != -1)
					{
						DrawingSVG d = m_FrameServer.Metadata.Keyframes[selectedFrame].Drawings[selectedDrawing] as DrawingSVG;
						if(d != null)
						{
							d.ResizeFinished();
						}
					}
				
				}
				
				// Unselect drawings.
				//m_FrameServer.Metadata.SelectedDrawingFrame = -1;
				//m_FrameServer.Metadata.SelectedDrawing = -1;
							
				if (m_FrameServer.Metadata.SelectedDrawingFrame != -1 && m_FrameServer.Metadata.SelectedDrawing != -1)
				{
					m_DeselectionTimer.Start();					
				}
				
				pbSurfaceScreen.Invalidate();
			}
		}
		private void SurfaceScreen_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			if(m_FrameServer.IsConnected && e.Button == MouseButtons.Left)
			{
				OnPoke();
				
				Point descaledMouse = m_FrameServer.CoordinateSystem.Untransform(e.Location);
				m_FrameServer.Metadata.AllDrawingTextToNormalMode();
				m_FrameServer.Metadata.UnselectAll();
				
				//------------------------------------------------------------------------------------
				// - If on text, switch to edit mode.
				// - If on other drawing, launch the configuration dialog.
				// - Otherwise -> Maximize/Reduce image.
				//------------------------------------------------------------------------------------
				if (m_FrameServer.Metadata.IsOnDrawing(0, descaledMouse, 0))
				{
					AbstractDrawing ad = m_FrameServer.Metadata.Keyframes[0].Drawings[m_FrameServer.Metadata.SelectedDrawing];
					if (ad is DrawingText)
					{
						((DrawingText)ad).EditMode = true;
						m_ActiveTool = DrawingToolType.Text;
						m_bTextEdit = true;
					}
					else if(!(ad is DrawingSVG))
					{
						mnuConfigureDrawing_Click(null, EventArgs.Empty);
					}
				}
				else if (m_FrameServer.Metadata.IsOnGrid(descaledMouse))
				{
					mnuGridsConfigure_Click(null, EventArgs.Empty);
				}
				else
				{
					ToggleStretchMode();
				}
			}
		}
		private void SurfaceScreen_Paint(object sender, PaintEventArgs e)
		{
			// Draw the image.
			m_FrameServer.Draw(e.Graphics);
						
			// Draw selection Border if needed.
			if (m_bShowImageBorder)
			{
				DrawImageBorder(e.Graphics);
			}	
		}
		private void SurfaceScreen_MouseEnter(object sender, EventArgs e)
		{
			
			// Set focus to surfacescreen to enable mouse scroll
			
			// But only if there is no Text edition going on.
			bool bEditing = false;
			
			foreach (AbstractDrawing ad in m_FrameServer.Metadata[0].Drawings)
			{
				DrawingText dt = ad as DrawingText;
				if (dt != null)
				{
					if(dt.EditMode)
					{
						bEditing = true;
						break;
					}
				}
			}
			
			
			if(!bEditing)
			{
				pbSurfaceScreen.Focus();
			}
		}
		
		#endregion

		#region PanelCenter Events
		private void PanelCenter_MouseEnter(object sender, EventArgs e)
		{
			// Give focus to enable mouse scroll.
			panelCenter.Focus();
		}
		private void PanelCenter_MouseClick(object sender, MouseEventArgs e)
		{
			OnPoke();
		}
		private void PanelCenter_Resize(object sender, EventArgs e)
		{
			StretchSqueezeSurface();
			pbSurfaceScreen.Invalidate();
		}
		private void PanelCenter_MouseDown(object sender, MouseEventArgs e)
		{
			panelCenter.ContextMenuStrip = popMenu;
		}
		#endregion
		
		#region Keyframes Panel
		private void pnlThumbnails_MouseEnter(object sender, EventArgs e)
		{
			// Give focus to disable keyframe box editing.
			pnlThumbnails.Focus();
		}
		private void splitKeyframes_Resize(object sender, EventArgs e)
		{
			// Redo the dock/undock if needed to be at the right place.
			// (Could be handled by layout ?)
			DockKeyframePanel(m_bDocked);
		}
		private void SetupDefaultThumbBox(UserControl _box)
		{
			_box.Top = 10;
			_box.Cursor = Cursors.Hand;
		}
		public void OnKeyframesTitleChanged()
		{
			// Called when title changed.
			pbSurfaceScreen.Invalidate();
		}
		private void pnlThumbnails_DoubleClick(object sender, EventArgs e)
		{
			OnPoke();
		}

		#region Docking Undocking
		private void btnDockBottom_Click(object sender, EventArgs e)
		{
			DockKeyframePanel(!m_bDocked);
		}
		private void splitKeyframes_Panel2_DoubleClick(object sender, EventArgs e)
		{
			DockKeyframePanel(!m_bDocked);
		}
		private void DockKeyframePanel(bool _bDock)
		{
			if(_bDock)
			{
				// hide the keyframes, change image.
				splitKeyframes.SplitterDistance = splitKeyframes.Height - 25;				
				btnDockBottom.BackgroundImage = Resources.undock16x16;
				btnDockBottom.Visible = m_FrameServer.RecentlyCapturedVideos.Count > 0;
			}
			else
			{
				// show the keyframes, change image.
				splitKeyframes.SplitterDistance = splitKeyframes.Height - 140;
				btnDockBottom.BackgroundImage = Resources.dock16x16;
				btnDockBottom.Visible = true;
			}
			
			m_bDocked = _bDock;
		}
		#endregion

		#endregion

		#region Drawings Toolbar Events
		private void btnDrawingToolLine2D_Click(object sender, EventArgs e)
		{
			if (m_FrameServer.Magnifier.Mode != MagnifierMode.Direct)
			{
				OnPoke();
				m_ActiveTool = DrawingToolType.Line2D;
				SetCursor(m_DrawingTools[(int)m_ActiveTool].GetCursor(m_ColorProfile.ColorLine2D, 0));
			}
		}
		private void btnDrawingToolPointer_Click(object sender, EventArgs e)
		{
			OnPoke();
			m_ActiveTool = DrawingToolType.Pointer;
			SetCursor(m_DrawingTools[(int)m_ActiveTool].GetCursor(Color.Empty, 0));
		}
		private void btnDrawingToolCross2D_Click(object sender, EventArgs e)
		{
			if (m_FrameServer.Magnifier.Mode != MagnifierMode.Direct)
			{
				OnPoke();
				m_ActiveTool = DrawingToolType.Cross2D;
				SetCursor(m_DrawingTools[(int)m_ActiveTool].GetCursor(m_ColorProfile.ColorCross2D, 0));
			}
		}
		private void btnDrawingToolAngle2D_Click(object sender, EventArgs e)
		{
			if (m_FrameServer.Magnifier.Mode != MagnifierMode.Direct)
			{
				OnPoke();
				m_ActiveTool = DrawingToolType.Angle2D;
				SetCursor(m_DrawingTools[(int)m_ActiveTool].GetCursor(m_ColorProfile.ColorAngle2D, 0));
			}
		}
		private void btnDrawingToolPencil_Click(object sender, EventArgs e)
		{
			if (m_FrameServer.Magnifier.Mode != MagnifierMode.Direct)
			{
				OnPoke();
				m_ActiveTool = DrawingToolType.Pencil;
				UpdateCursor();
			}
		}
		private void btnMagnifier_Click(object sender, EventArgs e)
		{
			if (m_FrameServer.IsConnected)
			{
				m_ActiveTool = DrawingToolType.Pointer;

				// Magnifier is half way between a persisting tool (like trackers and chronometers).
				// and a mode like grid and 3dplane.
				if (m_FrameServer.Magnifier.Mode == MagnifierMode.NotVisible)
				{
					UnzoomDirectZoom();
					m_FrameServer.Magnifier.Mode = MagnifierMode.Direct;
					btnMagnifier.BackgroundImage = Resources.magnifierActive2;
					SetCursor(Cursors.Cross);
				}
				else if (m_FrameServer.Magnifier.Mode == MagnifierMode.Direct)
				{
					// Revert to no magnification.
					UnzoomDirectZoom();
					m_FrameServer.Magnifier.Mode = MagnifierMode.NotVisible;
					btnMagnifier.BackgroundImage = Resources.magnifier2;
					SetCursor(m_DrawingTools[(int)DrawingToolType.Pointer].GetCursor(Color.Empty, 0));
					pbSurfaceScreen.Invalidate();
				}
				else
				{
					DisableMagnifier();
					pbSurfaceScreen.Invalidate();
				}
			}
		}
		private void DisableMagnifier()
		{
			// Revert to no magnification.
			m_FrameServer.Magnifier.Mode = MagnifierMode.NotVisible;
			btnMagnifier.BackgroundImage = Resources.magnifier2;
			SetCursor(m_DrawingTools[(int)DrawingToolType.Pointer].GetCursor(Color.Empty, 0));
		}
		private void btn3dplane_Click(object sender, EventArgs e)
		{
			m_FrameServer.Metadata.Plane.Visible = !m_FrameServer.Metadata.Plane.Visible;
			m_ActiveTool = DrawingToolType.Pointer;
			OnPoke();
			pbSurfaceScreen.Invalidate();
		}
		private void UpdateCursor()
		{
			// Ther current cursor must be updated.

			// Get the cursor and use it.
			if (m_ActiveTool == DrawingToolType.Pencil)
			{
				int iCircleSize = (int)((double)m_ColorProfile.StylePencil.Size * m_FrameServer.CoordinateSystem.Stretch);
				Cursor c = m_DrawingTools[(int)m_ActiveTool].GetCursor(m_ColorProfile.ColorPencil, iCircleSize);
				SetCursor(c);
			}
			else if (m_ActiveTool == DrawingToolType.Cross2D)
			{
				Cursor c = m_DrawingTools[(int)m_ActiveTool].GetCursor(m_ColorProfile.ColorCross2D, 0);
				SetCursor(c);
			}
		}
		private void btnDrawingToolText_Click(object sender, EventArgs e)
		{
			if (m_FrameServer.Magnifier.Mode != MagnifierMode.Direct)
			{
				OnPoke();
				m_ActiveTool = DrawingToolType.Text;
				SetCursor(m_DrawingTools[(int)m_ActiveTool].GetCursor(Color.Empty, 0));
			}
		}
		private void btnColorProfile_Click(object sender, EventArgs e)
		{
			OnPoke();

			// Load, save or modify current profile.
			formColorProfile fcp = new formColorProfile(m_ColorProfile);
			fcp.ShowDialog();
			fcp.Dispose();

			UpdateCursor();
		}
		private void SetCursor(Cursor _cur)
		{
			if (m_ActiveTool != DrawingToolType.Pointer)
			{
				panelCenter.Cursor = _cur;
			}
			else
			{
				panelCenter.Cursor = Cursors.Default;
			}

			pbSurfaceScreen.Cursor = _cur;
		}
		private void LocateForm(Form _form)
		{
			// A helper function to center the dialog boxes.
			if (Cursor.Position.X + (_form.Width / 2) >= SystemInformation.PrimaryMonitorSize.Width)
			{
				_form.StartPosition = FormStartPosition.CenterScreen;
			}
			else
			{
				_form.Location = new Point(Cursor.Position.X - (_form.Width / 2), Cursor.Position.Y - 20);
			}
		}
		#endregion

		#region Context Menus Events
		
		#region Drawings Menus
		private void mnuConfigureDrawing_Click(object sender, EventArgs e)
		{
			if(m_FrameServer.Metadata.SelectedDrawing >= 0)
			{
				formConfigureDrawing fcd = new formConfigureDrawing(m_FrameServer.Metadata[0].Drawings[m_FrameServer.Metadata.SelectedDrawing], pbSurfaceScreen);
				LocateForm(fcd);
				fcd.ShowDialog();
				fcd.Dispose();
				pbSurfaceScreen.Invalidate();
				this.ContextMenuStrip = popMenu;
			}
		}
		private void mnuConfigureFading_Click(object sender, EventArgs e)
		{
			if(m_FrameServer.Metadata.SelectedDrawing >= 0)
			{
				formConfigureFading fcf = new formConfigureFading(m_FrameServer.Metadata[0].Drawings[m_FrameServer.Metadata.SelectedDrawing], pbSurfaceScreen);
				LocateForm(fcf);
				fcf.ShowDialog();
				fcf.Dispose();
				pbSurfaceScreen.Invalidate();
				this.ContextMenuStrip = popMenu;
			}
		}
		private void mnuShowMeasure_Click(object sender, EventArgs e)
		{
			// Enable / disable the display of the measure for this line.
			if(m_FrameServer.Metadata.SelectedDrawing >= 0)
			{
				DrawingLine2D line = m_FrameServer.Metadata[0].Drawings[m_FrameServer.Metadata.SelectedDrawing] as DrawingLine2D;
				if(line!= null)
				{
					mnuShowMeasure.Checked = !mnuShowMeasure.Checked;
					line.ShowMeasure = mnuShowMeasure.Checked;
					m_bMeasuring = mnuShowMeasure.Checked;
					pbSurfaceScreen.Invalidate();
				}
			}
		}
		private void mnuSealMeasure_Click(object sender, EventArgs e)
		{
			// display a dialog that let the user specify how many real-world-units long is this line.
			
			if(m_FrameServer.Metadata.SelectedDrawing >= 0)
			{
				DrawingLine2D line = m_FrameServer.Metadata[0].Drawings[m_FrameServer.Metadata.SelectedDrawing] as DrawingLine2D;
				if(line!= null)
				{
					if(line.m_StartPoint.X != line.m_EndPoint.X || line.m_StartPoint.Y != line.m_EndPoint.Y)
					{
						if(!line.ShowMeasure)
							line.ShowMeasure = true;
						
						m_bMeasuring = true;
						
						DelegatesPool dp = DelegatesPool.Instance();
						if (dp.DeactivateKeyboardHandler != null)
						{
							dp.DeactivateKeyboardHandler();
						}

						formConfigureMeasure fcm = new formConfigureMeasure(m_FrameServer.Metadata, line);
						LocateForm(fcm);
						fcm.ShowDialog();
						fcm.Dispose();
						
						pbSurfaceScreen.Invalidate();
						this.ContextMenuStrip = popMenu;
						
						if (dp.ActivateKeyboardHandler != null)
						{
							dp.ActivateKeyboardHandler();
						}
					}
				}
			}
		}
		private void mnuDeleteDrawing_Click(object sender, EventArgs e)
		{
			DeleteSelectedDrawing();
			this.ContextMenuStrip = popMenu;
		}
		private void DeleteSelectedDrawing()
		{
			if (m_FrameServer.Metadata.SelectedDrawing >= 0)
			{
				IUndoableCommand cdd = new CommandDeleteDrawing(DoInvalidate, m_FrameServer.Metadata, m_FrameServer.Metadata[0].Position, m_FrameServer.Metadata.SelectedDrawing);
				CommandManager cm = CommandManager.Instance();
				cm.LaunchUndoableCommand(cdd);
				pbSurfaceScreen.Invalidate();
				this.ContextMenuStrip = popMenu;
			}
		}
		#endregion
		
		#region Magnifier Menus
		private void mnuMagnifierQuit_Click(object sender, EventArgs e)
		{
			DisableMagnifier();
			pbSurfaceScreen.Invalidate();
		}
		private void mnuMagnifierDirect_Click(object sender, EventArgs e)
		{
			// Use position and magnification to Direct Zoom.
			// Go to direct zoom, at magnifier zoom factor, centered on same point as magnifier.
			m_FrameServer.CoordinateSystem.Zoom = m_FrameServer.Magnifier.ZoomFactor;
			m_FrameServer.CoordinateSystem.RelocateZoomWindow(m_FrameServer.Magnifier.MagnifiedCenter);
			DisableMagnifier();
			m_FrameServer.Metadata.ResizeFinished();
			pbSurfaceScreen.Invalidate();
		}
		private void mnuMagnifier150_Click(object sender, EventArgs e)
		{
			SetMagnifier(mnuMagnifier150, 1.5);
		}
		private void mnuMagnifier175_Click(object sender, EventArgs e)
		{
			SetMagnifier(mnuMagnifier175, 1.75);
		}
		private void mnuMagnifier200_Click(object sender, EventArgs e)
		{
			SetMagnifier(mnuMagnifier200, 2.0);
		}
		private void mnuMagnifier225_Click(object sender, EventArgs e)
		{
			SetMagnifier(mnuMagnifier225, 2.25);
		}
		private void mnuMagnifier250_Click(object sender, EventArgs e)
		{
			SetMagnifier(mnuMagnifier250, 2.5);
		}
		private void SetMagnifier(ToolStripMenuItem _menu, double _fValue)
		{
			m_FrameServer.Magnifier.ZoomFactor = _fValue;
			UncheckMagnifierMenus();
			_menu.Checked = true;
			pbSurfaceScreen.Invalidate();
		}
		private void UncheckMagnifierMenus()
		{
			mnuMagnifier150.Checked = false;
			mnuMagnifier175.Checked = false;
			mnuMagnifier200.Checked = false;
			mnuMagnifier225.Checked = false;
			mnuMagnifier250.Checked = false;
		}
		#endregion

		#region Grids Menus
		private void mnuGridsConfigure_Click(object sender, EventArgs e)
		{
			formConfigureGrids fcg;

			if (m_FrameServer.Metadata.Plane.Selected)
			{
				m_FrameServer.Metadata.Plane.Selected = false;
				fcg = new formConfigureGrids(m_FrameServer.Metadata.Plane, pbSurfaceScreen);
				LocateForm(fcg);
				fcg.ShowDialog();
				fcg.Dispose();
			}
			else if (m_FrameServer.Metadata.Grid.Selected)
			{
				m_FrameServer.Metadata.Grid.Selected = false;
				fcg = new formConfigureGrids(m_FrameServer.Metadata.Grid, pbSurfaceScreen);
				LocateForm(fcg);
				fcg.ShowDialog();
				fcg.Dispose();
			}

			pbSurfaceScreen.Invalidate();
			
		}
		private void mnuGridsHide_Click(object sender, EventArgs e)
		{
			if (m_FrameServer.Metadata.Plane.Selected)
			{
				m_FrameServer.Metadata.Plane.Selected = false;
				m_FrameServer.Metadata.Plane.Visible = false;
			}
			else if (m_FrameServer.Metadata.Grid.Selected)
			{
				m_FrameServer.Metadata.Grid.Selected = false;
				m_FrameServer.Metadata.Grid.Visible = false;
			}

			pbSurfaceScreen.Invalidate();

			// Triggers an update to the menu.
			OnPoke();
		}
		#endregion

		#endregion
		
		#region DirectZoom
		private void UnzoomDirectZoom()
		{
			m_FrameServer.CoordinateSystem.ReinitZoom();
			((DrawingToolPointer)m_DrawingTools[(int)DrawingToolType.Pointer]).SetZoomLocation(m_FrameServer.CoordinateSystem.Location);
			m_FrameServer.Metadata.ResizeFinished();
		}
		private void IncreaseDirectZoom()
		{
			if (m_FrameServer.Magnifier.Mode != MagnifierMode.NotVisible)
			{
				DisableMagnifier();
			}

			// Max zoom : 600%
			if (m_FrameServer.CoordinateSystem.Zoom < 6.0f)
			{
				m_FrameServer.CoordinateSystem.Zoom += 0.20f;
				RelocateDirectZoom();
				m_FrameServer.Metadata.ResizeFinished();	
			}
			
			pbSurfaceScreen.Invalidate();
		}
		private void DecreaseDirectZoom()
		{
			if (m_FrameServer.CoordinateSystem.Zoom > 1.0f)
			{
				m_FrameServer.CoordinateSystem.Zoom -= 0.20f;
				RelocateDirectZoom();
				m_FrameServer.Metadata.ResizeFinished();
				pbSurfaceScreen.Invalidate();
			}
		}
		private void RelocateDirectZoom()
		{
			m_FrameServer.CoordinateSystem.RelocateZoomWindow();
			((DrawingToolPointer)m_DrawingTools[(int)DrawingToolType.Pointer]).SetZoomLocation(m_FrameServer.CoordinateSystem.Location);
		}
		#endregion
		
		#region Export video and frames
		private void btnBrowseImageLocation_Click(object sender, EventArgs e)
        {
        	// Select the image snapshot folder.	
        	SelectSavingDirectory(tbImageDirectory);
        }
		private void btnBrowseVideoLocation_Click(object sender, EventArgs e)
        {
        	// Select the video capture folder.	
			SelectSavingDirectory(tbVideoDirectory);
        }
		private void SelectSavingDirectory(TextBox _tb)
		{
			folderBrowserDialog.Description = ""; // todo.
            folderBrowserDialog.ShowNewFolderButton = true;
            folderBrowserDialog.RootFolder = Environment.SpecialFolder.Desktop;

            if(Directory.Exists(_tb.Text))
            {
               	folderBrowserDialog.SelectedPath = _tb.Text;
            }
            
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                _tb.Text = folderBrowserDialog.SelectedPath;
            }			
		}
		private void tbImageDirectory_TextChanged(object sender, EventArgs e)
        {
			if(!ValidateFilename(tbImageDirectory.Text, true))
        	{
        		AlertInvalidFilename();
        	}
        	else
        	{
        		PreferencesManager.Instance().CaptureImageDirectory = tbImageDirectory.Text;	
        	}
        }
        private void tbVideoDirectory_TextChanged(object sender, EventArgs e)
        {
        	if(!ValidateFilename(tbVideoDirectory.Text, true))
        	{
        		AlertInvalidFilename();
        	}
        	else
        	{
        		PreferencesManager.Instance().CaptureVideoDirectory = tbVideoDirectory.Text;	
        	}
        }
        private void tbImageFilename_TextChanged(object sender, EventArgs e)
        {
			if(!ValidateFilename(tbImageFilename.Text, true))
        	{
        		AlertInvalidFilename();
        	}
        }
        private void tbVideoFilename_TextChanged(object sender, EventArgs e)
        {
        	if(!ValidateFilename(tbVideoFilename.Text, true))
        	{
        		AlertInvalidFilename();
        	}
        }
		private void btnSnapShot_Click(object sender, EventArgs e)
		{
			// Export the current frame.
			if(m_FrameServer.IsConnected)
			{
				if(!ValidateFilename(tbImageFilename.Text, false))
				{
					AlertInvalidFilename();	
				}
				else if(Directory.Exists(tbImageDirectory.Text))
				{
					string filepath = tbImageDirectory.Text + "\\" + tbImageFilename.Text;
					
					// Check if file already exists.
					if(OverwriteOrCreateImage(filepath))
					{
						Bitmap outputImage = m_FrameServer.GetFlushedImage();
						
						ImageHelper.Save(filepath, outputImage);
						outputImage.Dispose();
						
						// Update the filename for the next snapshot.
						// If the filename was empty, we'll create it without saving.
						tbImageFilename.Text = CreateNewFilename(tbImageFilename.Text);
					}
				}
				else
				{
					btnBrowseImageLocation_Click(null, EventArgs.Empty);
				}	
			}
		}
		private void btnRecord_Click(object sender, EventArgs e)
        {
			if(m_FrameServer.IsConnected)
			{
				if(m_FrameServer.IsRecording)
				{
					m_FrameServer.StopRecording();
					EnableVideoFileEdit(true);
					
					// update file name.
					tbVideoFilename.Text = CreateNewFilename(tbVideoFilename.Text);
					
					DisplayAsRecording(false);
				}
				else
				{
					// Start exporting frames to a video.
				
					// Check that the destination folder exists.
					if(!ValidateFilename(tbVideoFilename.Text, false))
					{
						AlertInvalidFilename();	
					}
					else if(Directory.Exists(tbVideoDirectory.Text))
					{
						// no extension : mkv.
						// extension specified by user : honor it if supported, mkv otherwise.
						string filename = tbVideoFilename.Text;
						string filepath = tbVideoDirectory.Text + "\\" + filename;
						string filenameToLower = filename.ToLower();
						
						if(filename != "")
						{
							if(!filenameToLower.EndsWith("mkv") && !filenameToLower.EndsWith("mp4") && !filenameToLower.EndsWith("avi"))
							{
								filepath = filepath + ".mkv";	
							}
							
							// Check if file already exists.
							if(OverwriteOrCreateVideo(filepath))
							{
								m_FrameServer.CurrentCaptureFilePath = filepath;
								m_FrameServer.StartRecording(filepath);
								EnableVideoFileEdit(false);
								DisplayAsRecording(true);
							}							
						}
						else
						{
							tbVideoFilename.Text = CreateNewFilename("");	
						}
					}
					else
					{
						btnBrowseVideoLocation_Click(null, EventArgs.Empty);
					}	
				}
				
				OnPoke();
			}
        }
		#endregion
        
		#region Capture specifics
		private void btnCamSettings_Click(object sender, EventArgs e)
        {
			m_FrameServer.PromptDeviceSelector();
        }
        private void tmrCaptureDeviceDetector_Tick(object sender, EventArgs e)
        {
        	TryToConnect();
        }
        private void TryToConnect()
        {
        	// Try to connect to a device.
        	if(!m_FrameServer.IsConnected)
        	{
        		// Prevent reentry.
        		if(!m_bTryingToConnect)
        		{
        			m_bTryingToConnect = true;        			
        			m_FrameServer.NegociateDevice();       			
        			m_bTryingToConnect = false;
        			
        			if(m_FrameServer.IsConnected)
        			{
        				btnCamSettings.Enabled = true;
        			}
        		}
        	}
        }
        private string CreateNewFilename(string filename)
        {
        	//-------------------------------------------------------------------
        	// Create the next file name from the existing one.
        	// if the existing name has a number in it, we increment this number.
        	// if not, we create a suffix.
			//-------------------------------------------------------------------
        	
			string newFilename = "";
			
			if(filename == null || filename == "")
			{
				// Create the name from the current date.
				DateTime now = DateTime.Now;
				newFilename = String.Format("{0}-{1:00}-{2:00} - 1", now.Year, now.Month, now.Day);
			}
			else
			{
				// Find all numbers in the name, if any.
				Regex r = new Regex(@"\d+");
				MatchCollection mc = filename.EndsWith(".mp4") ? 
					r.Matches(Path.GetFileNameWithoutExtension(filename)) : r.Matches(filename);
	        	
				if(mc.Count > 0)
	        	{
	        		// Increment the last one.
	        		Match m = mc[mc.Count - 1];
	        		int number = int.Parse(m.Value);
	        		number++;
	        	
	        		// Todo: handle leading zeroes in the original.
	        		// (LastIndexOf("0") ?
	        		
	        		// Replace the number in the original.
	        		newFilename = r.Replace(filename, number.ToString(), 1, m.Index );
	        	}
	        	else
	        	{
	        		// No number found, add suffix between text and extension (works if no extension).
	        		newFilename = String.Format("{0} - 2{1}", 
	        		                            Path.GetFileNameWithoutExtension(filename), 
	        		                            Path.GetExtension(filename));
	        	}
			}
			
        	return newFilename;
        }
        private bool ValidateFilename(string _filename, bool _allowEmpty)
        {
        	// Validate filename chars.
        	bool bIsValid = false;
        	
        	if(_filename.Length == 0 && _allowEmpty)
        	{
        		// special case for when the user is currently typing.
        		bIsValid = true;
        	}
        	else
        	{
				try
				{
				  	new System.IO.FileInfo(_filename);
				  	bIsValid = true;
				}
				catch (ArgumentException)
				{
					// filename is empty, only white spaces or contains invalid chars.
					log.Error(String.Format("Capture filename has invalid characters. Proposed file was: {0}", _filename));
				}
				catch (NotSupportedException)
				{
					// filename contains a colon in the middle of the string.
					log.Error(String.Format("Capture filename has a colon in the middle. Proposed file was: {0}", _filename));
				}
        	}
			
			return bIsValid;
        }
        private void AlertInvalidFilename()
        {
        	string msgTitle = ScreenManagerLang.Error_Capture_InvalidFile_Title;
        	string msgText = ScreenManagerLang.Error_Capture_InvalidFile_Text.Replace("\\n", "\n");
        		
			MessageBox.Show(msgText, msgTitle, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        }
        private bool OverwriteOrCreateVideo(string _filepath)
        {
        	// Check if the specified video file exists, and asks the user if he wants to overwrite.
        	bool bOverwriteOrCreate = true;
        	if(File.Exists(_filepath))
        	{
        		string msgTitle = ScreenManagerLang.Error_Capture_FileExists_Title;
        		string msgText = String.Format(ScreenManagerLang.Error_Capture_FileExists_Text, _filepath).Replace("\\n", "\n");
        		
        		DialogResult dr = MessageBox.Show(msgText, msgTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
        		if(dr != DialogResult.Yes)
        		{
        			bOverwriteOrCreate = false;
        		}
        	}
        	
        	return bOverwriteOrCreate;
        }
        private bool OverwriteOrCreateImage(string _filepath)
        {
        	// Check if the specified image file exists, and asks the user if he wants to overwrite.
        	bool bOverwriteOrCreate = true;
        	string filepathTest = _filepath;
        	string filepathLower = _filepath.ToLower();
        	if(!filepathLower.ToLower().EndsWith(".bmp") && 
        	   !filepathLower.ToLower().EndsWith(".jpg") &&
        	   !filepathLower.ToLower().EndsWith(".jpeg") &&
        	   !filepathLower.ToLower().EndsWith(".png"))
        	{
        		filepathTest = _filepath + ".jpg";
        	}       	
        	
        	if(File.Exists(filepathTest))
        	{
        		string msgTitle = ScreenManagerLang.Error_Capture_FileExists_Title;
        		string msgText = String.Format(ScreenManagerLang.Error_Capture_FileExists_Text, filepathTest).Replace("\\n", "\n");
        		
        		DialogResult dr = MessageBox.Show(msgText, msgTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
        		if(dr != DialogResult.Yes)
        		{
        			bOverwriteOrCreate = false;
        		}
        	}
        	
        	return bOverwriteOrCreate;
        }
        private void FoldSettings(object sender, EventArgs e)
        {
        	if(m_bSettingsFold)
        	{
        		panelVideoControls.Height = 142;
        		btnFoldSettings.BackgroundImage = Resources.dock16x16;
        	}
        	else
        	{
        		panelVideoControls.Height = lblSettings.Top + lblSettings.Height;
        		btnFoldSettings.BackgroundImage = Resources.undock16x16;
        	}
        	
        	m_bSettingsFold = !m_bSettingsFold;	
        }
        private void EnableVideoFileEdit(bool _bEnable)
        {
        	tbVideoFilename.Enabled = _bEnable;
        	tbVideoDirectory.Enabled = _bEnable;
        	btnBrowseVideoLocation.Enabled = _bEnable;
			btnSaveVideoLocation.Enabled = _bEnable;        	
        }
        #endregion
        
	}
}
