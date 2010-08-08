using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GoAlongTheDots
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		const double MovementRate = 200;
		Rect CreatureBox;
		Rect CircleBox;
		Int64 LastTicks;

		Image imgCircle;
		Image imgCreature;

		System.Media.SoundPlayer OmNomSound;
		System.Media.SoundPlayer CheerSound;
		List<Image> dots;

		bool freezeMovement = false;

		public MainWindow()
		{
			InitializeComponent();
			dots = new List<Image>();
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			OmNomSound = new System.Media.SoundPlayer("media\\omnom.wav");
			OmNomSound.Load();

			CheerSound = new System.Media.SoundPlayer("media\\kidscheer.wav");
			CheerSound.Load();

			SetupCreature();
			SetupCircle();

			ResetPositions();

			LastTicks = DateTime.Now.Ticks;
			CompositionTarget.Rendering += new EventHandler(CompositionTarget_Rendering);
		}

		private void SetupCreature()
		{
			imgCreature = new Image();
			Container.Children.Add(imgCreature);
			BitmapImage creatureBitmap = (BitmapImage)Resources["Creature"];
			imgCreature.Source = creatureBitmap;
			imgCreature.Height = creatureBitmap.PixelHeight;
			imgCreature.Width = creatureBitmap.PixelWidth;
		}

		void SetupCircle()
		{
			imgCircle = new Image();
			Container.Children.Add(imgCircle);
			BitmapImage circleBitmap = (BitmapImage)Resources["Circle"];
			imgCircle.Source = circleBitmap;
			imgCircle.Height = circleBitmap.PixelHeight;
			imgCircle.Width = circleBitmap.PixelWidth;
		}

		void ResetPositions()
		{
			CreatureBox = new Rect(
				Container.ActualWidth - imgCreature.Width - 50,
				Container.ActualHeight - imgCreature.Height - 50,
				imgCreature.Width,
				imgCreature.Height);
			Random circleRng = new Random();

			CircleBox = new Rect(
				circleRng.NextDouble() * ((Container.ActualWidth / 3) - imgCircle.Width - 50) + 25,
				circleRng.NextDouble() * (Container.ActualHeight - imgCircle.Height - 50) + 25,
				imgCircle.Width,
				imgCircle.Height);

			// Draw a line of dots
			foreach(var dot in dots)
				Container.Children.Remove(dot);
			
			dots.Clear();

			BitmapImage dotBitmap = (BitmapImage)Resources["Dot"];

			Vector line = CircleBox.TopLeft - CreatureBox.BottomRight;
			double slope = (CreatureBox.BottomRight.Y - CircleBox.TopLeft.Y) / (CreatureBox.BottomRight.X - CircleBox.TopLeft.X);
			double intercept = CircleBox.TopLeft.Y - (slope * CircleBox.TopLeft.X);

			int segments = 5;
			for(int i = 1; i < segments; i++)
			{
				var pos = i * line.Length / segments;
				var y = (slope * pos) + intercept;
				var x = pos + CircleBox.X;

				Image dotImage = new Image
				{
					Source = dotBitmap,
					Width = dotBitmap.PixelWidth,
					Height = dotBitmap.PixelHeight,
				};
				dots.Add(dotImage);

				Container.Children.Add(dotImage);
				Canvas.SetLeft(dotImage, x);
				Canvas.SetTop(dotImage, y);
			}
		}

		void CompositionTarget_Rendering(object sender, EventArgs e)
		{
			imgCreature.SetValue(Canvas.TopProperty, CreatureBox.Top);
			imgCreature.SetValue(Canvas.LeftProperty, CreatureBox.Left);

			imgCircle.SetValue(Canvas.TopProperty, CircleBox.Top);
			imgCircle.SetValue(Canvas.LeftProperty, CircleBox.Left);

#if DEBUG
			tbCreatureCoords.Text = String.Format("{0:f0},{1:f0} ({2:f0},{3:f0})",
				CreatureBox.Top,
				CreatureBox.Left,
				imgCreature.GetValue(Canvas.TopProperty),
				imgCreature.GetValue(Canvas.LeftProperty));
#endif

			Int64 currentTicks = DateTime.Now.Ticks;
			double elapsedSeconds = (double)(currentTicks - LastTicks) / 10000000.0;
			LastTicks = currentTicks;

			HandleInput(elapsedSeconds);

			CheckCollisions();
		}

		private void CheckCollisions()
		{
			if(CreatureBox.IntersectsWith(CircleBox))
			{
				if(!dots.Any())
				{
					CheerSound.PlaySync();
					ResetPositions();
				}
			}

			foreach(var dot in dots.ToArray())
			{
				Rect dotRect = new Rect(Canvas.GetLeft(dot), Canvas.GetTop(dot), dot.Width, dot.Height);

				if(CreatureBox.IntersectsWith(dotRect))
				{
					OmNomSound.Play();
					Container.Children.Remove(dot);
					dots.Remove(dot);
				}
			}
		}

		private void HandleInput(double elapsedSeconds)
		{
			double movementFactor = MovementRate * elapsedSeconds;

			if(!freezeMovement)
			{
				if(Keyboard.IsKeyDown(Key.Left))
				{
					if(CreatureBox.Left - movementFactor > 0)
						CreatureBox.Offset(-movementFactor, 0);
				}

				if(Keyboard.IsKeyDown(Key.Right))
				{
					if(CreatureBox.Right + movementFactor < ActualWidth)
						CreatureBox.Offset(movementFactor, 0);
				}

				if(Keyboard.IsKeyDown(Key.Up))
				{
					if(CreatureBox.Top - movementFactor < ActualHeight)
						CreatureBox.Offset(0, -movementFactor);
				}

				if(Keyboard.IsKeyDown(Key.Down))
				{
					if(CreatureBox.Bottom + movementFactor < ActualHeight)
						CreatureBox.Offset(0, movementFactor);
				}
			}
		}

		private void Window_KeyUp(object sender, KeyEventArgs e)
		{
			switch(e.Key)
			{
				case Key.F3:
					ResetPositions();
					break;

				case Key.Escape:
					Close();
					break;
			}

		}
	}
}
