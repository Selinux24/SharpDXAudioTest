﻿using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SharpDXAudioTest
{
    public partial class FormMain : Form
    {
        AudioEngine audioState;
        int frameToApply3DAudio = 0;
        TimeSpan gameTime = DateTime.Now.TimeOfDay;
        bool resume = false;
        float elapsedTime = 0;
        bool startAudio = true;

        ListenerInstance listenerInstance = null;

        AudioEffect helicopter = null;
        EmitterInstance emitterHelicopter = null;

        AudioEffect music = null;
        EmitterInstance emitterMusic = null;

        float masterVolume = 0;
        int pan = 0;
        int pitch = 0;
        float musicVolume = 0;
        float helicopterVolume = 0;

        private class ToUpdate3DVoice
        {
            public AudioEffect Voice { get; set; }
            public EmitterInstance Emitter { get; set; }
        }

        readonly List<IAgent> agents = new List<IAgent>();
        readonly List<ToUpdate3DVoice> voices3d = new List<ToUpdate3DVoice>();

        private static void MoveAgent(IAgent agent, Vector3 posDelta)
        {
            var pos = agent.Position + posDelta;

            pos.X = Math.Min(AudioConstants.XMAX, pos.X);
            pos.X = Math.Max(AudioConstants.XMIN, pos.X);
            pos.Z = Math.Min(AudioConstants.ZMAX, pos.Z);
            pos.Z = Math.Max(AudioConstants.ZMIN, pos.Z);

            if (agent.Position == pos)
            {
                return;
            }

            var vDelta = pos - agent.Position;
            vDelta.Y = 0f;

            agent.Position = pos;
            agent.OrientFront = Vector3.Normalize(vDelta);
        }
        private static string FormatOrientation(Vector3 orientation)
        {
            if (orientation == Vector3.ForwardLH)
            {
                return "Down"; // Form direction, not 3D real directio
            }
            else if (orientation == Vector3.BackwardLH)
            {
                return "Up"; // Form direction, not 3D real directio
            }
            else if (orientation == Vector3.Left)
            {
                return "Left";
            }
            else if (orientation == Vector3.Right)
            {
                return "Right";
            }
            else
            {
                return FormatPosition(orientation);
            }
        }
        private static string FormatPosition(Vector3 position)
        {
            return $"{ position.X:00},{ position.Y:00},{ position.Z:00}";
        }
        private static string FormatMatrix(float[] matrix)
        {
            return string.Join(" ", matrix.Select(m => $"{m:0.00}"));
        }

        public FormMain()
        {
            InitializeComponent();
        }

        private void FormMain_Load(object sender, EventArgs e)
        {
            InitUI();

            audioState = new AudioEngine(44100);
            audioState.Volume = 0.5f;

            music = audioState.InitializeEffect("Valquirias.mp3", true);
            helicopter = audioState.InitializeEffect("heli.wav", true);

            InitAgents();

            masterVolume = audioState.Volume * 100f;
            musicVolume = music.Volume * 100f;
            helicopterVolume = helicopter.Volume * 100f;

            tbMasterVolume.Value = (int)masterVolume;
            tbPan.Value = pan;
            tbPitch.Value = pitch;
            tbMusic.Value = (int)musicVolume;
            tbHelicopter.Value = (int)helicopterVolume;
        }
        private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            CleanupAudio();
        }
        private void ButPauseResume_Click(object sender, EventArgs e)
        {
            PauseAudio(resume);
            resume = !resume;
        }
        private void CmbEffects_SelectedValueChanged(object sender, EventArgs e)
        {
            if (audioState?.Initialized != true)
            {
                return;
            }

            ReverbPresets? value = null;
            int index = cbEffects.SelectedIndex - 1;
            if (index >= 0)
            {
                value = (ReverbPresets)index;
            }

            if (!helicopter.SetReverb(value))
            {
                MessageBox.Show($"Set reverb error - helicopter in {value}");
            }

            if (!music.SetReverb(value))
            {
                MessageBox.Show($"Set reverb error - music in {value}");
            }
        }
        private void ButUp_Click(object sender, EventArgs e)
        {
            var agent = agents.FirstOrDefault(a => a.Name == (string)cbAgent.SelectedItem);
            if (agent == null)
            {
                return;
            }

            MoveAgent(agent, Vector3.BackwardLH);
            this.Invalidate();
        }
        private void ButRight_Click(object sender, EventArgs e)
        {
            var agent = agents.FirstOrDefault(a => a.Name == (string)cbAgent.SelectedItem);
            if (agent == null)
            {
                return;
            }

            MoveAgent(agent, Vector3.Right);
            this.Invalidate();
        }
        private void ButDown_Click(object sender, EventArgs e)
        {
            var agent = agents.FirstOrDefault(a => a.Name == (string)cbAgent.SelectedItem);
            if (agent == null)
            {
                return;
            }

            MoveAgent(agent, Vector3.ForwardLH);
            this.Invalidate();
        }
        private void ButLeft_Click(object sender, EventArgs e)
        {
            var agent = agents.FirstOrDefault(a => a.Name == (string)cbAgent.SelectedItem);
            if (agent == null)
            {
                return;
            }

            MoveAgent(agent, Vector3.Left);
            this.Invalidate();
        }
        private void ChkListenerCone_CheckedChanged(object sender, EventArgs e)
        {
            listenerInstance.UseCone = !listenerInstance.UseCone;
            this.Invalidate();
        }
        private void ChkListenerInnerRadius_CheckedChanged(object sender, EventArgs e)
        {
            listenerInstance.UseInnerRadius = !listenerInstance.UseInnerRadius;
            this.Invalidate();
        }
        private void TbMasterVolume_Scroll(object sender, EventArgs e)
        {
            masterVolume = tbMasterVolume.Value / (float)tbMasterVolume.Maximum * 100f;

            audioState.Volume = masterVolume / 100f;
        }
        private void TbPan_Scroll(object sender, EventArgs e)
        {
            pan = tbPan.Value;

            helicopter.Pan = pan / 50f;
            music.Pan = pan / 50f;
        }
        private void TbPitch_Scroll(object sender, EventArgs e)
        {
            pitch = tbPitch.Value;

            helicopter.Pitch = pitch / 50f;
            music.Pitch = pitch / 50f;
        }
        private void TbMusic_Scroll(object sender, EventArgs e)
        {
            musicVolume = tbMusic.Value / (float)tbMusic.Maximum * 100f;

            music.Volume = musicVolume / 100f;
        }
        private void TbHelicopter_Scroll(object sender, EventArgs e)
        {
            helicopterVolume = tbHelicopter.Value / (float)tbHelicopter.Maximum * 100f;

            helicopter.Volume = helicopterVolume / 100f;
        }
        private void TimerUpdate_Tick(object sender, EventArgs e)
        {
            try
            {
                TimeSpan prevTime = gameTime;
                gameTime = DateTime.Now.TimeOfDay;
                elapsedTime = (float)(gameTime - prevTime).TotalSeconds;

                if (!UpdateAudio(elapsedTime))
                {
                    this.txtData.Text = "UpdateAudio error";
                    return;
                }

                UpdateText();

                if (startAudio)
                {
                    helicopter.Play();
                    music.Play();

                    startAudio = false;
                }
            }
            catch (Exception ex)
            {
                this.txtData.Text = $"TimerUpdate error. {ex.Message}";
            }
        }

        private void UpdateText()
        {
            var helicopterDsp = helicopter.GetOutputMatrix();
            var musicDsp = music.GetOutputMatrix();

            string helicopterText = $"Helicopter      Pos {FormatPosition(emitterHelicopter.Position)}";
            string helicopterDspText = $"Helicopter DSP.   {FormatMatrix(helicopterDsp)}";
            string musicText = $"Music           Pos {FormatPosition(emitterMusic.Position)}";
            string musicDspText = $"Music DSP.        {FormatMatrix(musicDsp)}";
            string listenerText = $"Listener        Pos {FormatPosition(listenerInstance.Position)}";
            string listenerText2 = $"Listener        Dir {FormatOrientation(listenerInstance.OrientFront)}";
            string musicVolumeText = $"Music volume      {musicVolume:000}";
            string helicopterVolumeText = $"Helicopter volume {helicopterVolume:000}";
            string masterVolumeText = $"Master volume     {masterVolume:000}";

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(helicopterText);
            sb.AppendLine(musicText);
            sb.AppendLine(listenerText);
            sb.AppendLine(listenerText2);
            sb.AppendLine();
            sb.AppendLine(helicopterDspText);
            sb.AppendLine(musicDspText);
            sb.AppendLine();
            sb.AppendLine(musicVolumeText);
            sb.AppendLine(helicopterVolumeText);
            sb.AppendLine(masterVolumeText);

            if (this.InvokeRequired)
            {
                this.Invoke(new MethodInvoker(() =>
                {
                    this.txtData.Text = sb.ToString();
                }));
            }
            else
            {
                this.txtData.Text = sb.ToString();
            }
        }

        void InitUI()
        {
            var propNames = AudioConstants.GetPresetNames();

            this.cbEffects.Items.AddRange(propNames.ToArray());
            this.cbEffects.Items.Insert(0, "None");
            this.cbEffects.SelectedIndex = 0;

            listenerInstance = new ListenerInstance
            {
                Name = "Listener",
                Position = Vector3.Zero,
            };
            emitterHelicopter = new EmitterInstance
            {
                Name = "Helicopter",
                Position = new Vector3(0f, 0f, AudioConstants.ZMAX),
            };
            emitterMusic = new EmitterInstance
            {
                Name = "Music",
                Position = new Vector3(AudioConstants.XMAX, 0f, AudioConstants.ZMAX),
            };
            agents.AddRange(new IAgent[] { listenerInstance, emitterMusic, emitterHelicopter });

            this.cbAgent.Items.AddRange(agents.Select(a => a.Name).ToArray());
            this.cbAgent.SelectedIndex = 0;
        }
        void InitAgents()
        {
            helicopter.Initialize3D(audioState, emitterHelicopter, listenerInstance);
            voices3d.Add(new ToUpdate3DVoice { Voice = helicopter, Emitter = emitterHelicopter });

            music.Initialize3D(audioState, emitterMusic, listenerInstance);
            voices3d.Add(new ToUpdate3DVoice { Voice = music, Emitter = emitterMusic });
        }
        bool UpdateAudio(float elapsedSeconds)
        {
            if (!audioState.Initialized)
            {
                return false;
            }

            if (frameToApply3DAudio < voices3d.Count)
            {
                var voice3d = voices3d[frameToApply3DAudio];
                voice3d.Voice.Apply3D(elapsedSeconds);
            }

            frameToApply3DAudio = ++frameToApply3DAudio % voices3d.Count;

            return true;
        }
        void PauseAudio(bool resume)
        {
            if (!audioState.Initialized)
            {
                return;
            }

            if (resume)
            {
                audioState.Start();
            }
            else
            {
                audioState.Stop();
            }
        }
        void CleanupAudio()
        {
            voices3d.Clear();

            helicopter?.Dispose();
            helicopter = null;

            music?.Dispose();
            music = null;

            audioState?.Dispose();
            audioState = null;
        }

        void DrawCanvas()
        {
            var agent = agents.FirstOrDefault(a => a.Name == (string)cbAgent.SelectedItem);
            System.Drawing.Color agentColor = System.Drawing.Color.Black;

            using (var myBrush = new System.Drawing.SolidBrush(System.Drawing.Color.Red))
            using (var myPen = new System.Drawing.Pen(System.Drawing.Color.Black))
            using (var formGraphics = panCanvas.CreateGraphics())
            {
                formGraphics.Clear(System.Drawing.Color.White);

                int radius = (int)(panCanvas.ClientRectangle.Width * 1.22f);

                myBrush.Color = System.Drawing.Color.CornflowerBlue;
                if (listenerInstance.UseCone)
                {
                    float coneAngle = MathUtil.RadiansToDegrees(listenerInstance.Cone.OuterAngle - listenerInstance.Cone.InnerAngle);

                    float dirAngle = MathUtil.RadiansToDegrees(AngleSigned(Vector2.UnitX, new Vector2(listenerInstance.OrientFront.X, listenerInstance.OrientFront.Z)));
                    dirAngle -= coneAngle * 0.5f;
                    if (dirAngle < 0) dirAngle += 360;

                    myPen.Color = System.Drawing.Color.Blue;
                    formGraphics.FillPie(
                        myBrush,
                        GetItemRectangle(listenerInstance.Position, 50),
                        dirAngle,
                        coneAngle);
                }
                if (agent == listenerInstance) agentColor = System.Drawing.Color.Blue;
                myBrush.Color = System.Drawing.Color.Blue;
                formGraphics.FillEllipse(myBrush, GetItemRectangle(listenerInstance.Position, 10));

                if (agent == emitterHelicopter) agentColor = System.Drawing.Color.Red;
                myBrush.Color = System.Drawing.Color.Red;
                myPen.Color = System.Drawing.Color.Red;
                formGraphics.FillEllipse(myBrush, GetItemRectangle(emitterHelicopter.Position, 10));
                formGraphics.DrawEllipse(myPen, GetItemRectangle(emitterHelicopter.Position, radius));

                if (agent == emitterMusic) agentColor = System.Drawing.Color.DarkGray;
                myBrush.Color = System.Drawing.Color.DarkGray;
                myPen.Color = System.Drawing.Color.DarkGray;
                formGraphics.FillEllipse(myBrush, GetItemRectangle(emitterMusic.Position, 10));
                formGraphics.DrawEllipse(myPen, GetItemRectangle(emitterMusic.Position, radius));

                myPen.Color = agentColor;
                formGraphics.DrawEllipse(myPen, GetItemRectangle(agent.Position, 15));
            }
        }
        private System.Drawing.Rectangle GetItemRectangle(Vector3 p, int size)
        {
            var bounds = panCanvas.ClientRectangle;

            float xSize = AudioConstants.XMAX - AudioConstants.XMIN;
            float zSize = AudioConstants.ZMAX - AudioConstants.ZMIN;

            var px = (p.X + AudioConstants.XMAX) * (bounds.Width - xSize) / xSize;
            var pz = (p.Z + AudioConstants.ZMAX) * (bounds.Height - zSize) / zSize;
            px += xSize * 0.5f;
            pz += zSize * 0.5f;

            return new System.Drawing.Rectangle((int)(px - (size * 0.5f)), (int)(pz - (size * 0.5f)), size, size);
        }

        private static float Cross(Vector2 one, Vector2 two)
        {
            return one.X * two.Y - one.Y * two.X;
        }
        private static float AngleSigned(Vector2 one, Vector2 two)
        {
            return (float)Math.Atan2(Cross(one, two), Vector2.Dot(one, two));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            DrawCanvas();
        }
    }
}
