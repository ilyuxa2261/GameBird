using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia; 
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace GameBird;

public partial class Game : Window
{
    private DispatcherTimer _fallTimer = null!;
    private DispatcherTimer _obstacleTimer = null!;
    private DispatcherTimer _timer = null!;
    private const double Gravity = 10;
    private const double JumpForce = -40;
    private readonly Random _random = new();
    private const int MinGap = 100;
    private const int MaxGap = 200;
    private const int ObstacleWidth = 60;
    private const double ObstacleSpeed = 10;
    private List<Border> _obstacles = new();
    private int Count;

    public Game()
    {
        InitializeComponent();
        fall_bird();
        generate_obstacle();
        timer_count();
        Raw();
        
    }

    private void fall_bird()
    {
        _fallTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        _fallTimer.Tick += OnFallTick!;
        _fallTimer.Start();
    }

    private void generate_obstacle()
    {
        _obstacleTimer = new DispatcherTimer();
        _obstacleTimer.Interval = TimeSpan.FromMilliseconds(_random.Next(3000, 7000));
        _obstacleTimer.Tick += SpawnObstacle!;
        _obstacleTimer.Start();
    }

    private void Raw()
    {
        AddHandler(PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
    }

    private void timer_count()
    {
        
        Count = 0;
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += Timer_Tick;
        _timer.Start();
    }


    private void OnFallTick(object sender, EventArgs e)
    {
        var margin = GoldSquare.Margin;
        GoldSquare.Margin = new Thickness(
            margin.Left,
            margin.Top + Gravity,
            margin.Right,
            margin.Bottom
        );
        
        
        for (int i = _obstacles.Count - 1; i >= 0; i--)
        {
            var obstacle = _obstacles[i];
            var obsMargin = obstacle.Margin;
            obstacle.Margin = new Thickness(
                obsMargin.Left - ObstacleSpeed,
                obsMargin.Top,
                obsMargin.Right,
                obsMargin.Bottom
            );
            
            if (obstacle.Margin.Left + ObstacleWidth < 0)
            {
                MainGrid.Children.Remove(obstacle);
                _obstacles.RemoveAt(i);
            }
        }
        
        CheckCollisions();
    }

    
    
    
    private void SpawnObstacle(object sender, EventArgs e)
    {
        _obstacleTimer.Interval = TimeSpan.FromMilliseconds(_random.Next(3000, 7000));
        int gapY = _random.Next(0, (int)Height - MinGap);
        int gapHeight = _random.Next(MinGap, MaxGap);
    
        var topObstacle = new Border
        {
            Width = ObstacleWidth,
            Height = gapY,
            Background = Brushes.Green,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(Width, 0, 0, 0),
            IsHitTestVisible = false
        };
    
        var bottomObstacle = new Border
        {
            Width = ObstacleWidth,
            Height = Height - gapY - gapHeight,
            Background = Brushes.Green,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(Width, 0, 0, 0),
            IsHitTestVisible = false
        };
        MainGrid.Children.Add(topObstacle);
        MainGrid.Children.Add(bottomObstacle);
        
        _obstacles.Add(topObstacle);
        _obstacles.Add(bottomObstacle);
    }

    
    
    
    
    private void CheckCollisions()
    {
        var birdRect = new Rect(
            GoldSquare.Margin.Left,
            GoldSquare.Margin.Top,
            GoldSquare.Width,
            GoldSquare.Height);
        foreach (var obstacle in _obstacles)
        {
            var obstacleRect = new Rect(
                obstacle.Margin.Left,
                obstacle.VerticalAlignment == VerticalAlignment.Top 
                    ? 0 
                    : Height - obstacle.Height,
                obstacle.Width,
                obstacle.Height);
            
            if (birdRect.Intersects(obstacleRect))
            {
                GameOver(Count);
                return;
            }
        }
        
        if (GoldSquare.Margin.Top < 0 || GoldSquare.Margin.Top + GoldSquare.Height > Height)
        {
            GameOver(Count);
        }
    }

    
    
    
    private async void GameOver(int count)
    {
        _fallTimer.Stop();
        _obstacleTimer.Stop();
        
        var gameOverText = new TextBlock
        {
            Text = "GAME OVER",
            FontSize = 50,
            Foreground = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        
        MainGrid.Children.Add(gameOverText);
        
        
        string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Count.json");
        ScoreData scoreData;
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            scoreData = JsonSerializer.Deserialize<ScoreData>(json) ?? new ScoreData();
        }
        else
        {
            scoreData = new ScoreData();
        }
        
        scoreData.Now = Count;
        if (Count > scoreData.Top)
        {
            scoreData.Top = Count;
        }

        string updatedJson = JsonSerializer.Serialize(scoreData, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(filePath, updatedJson);

        await Task.Delay(2000);
        
        new MainWindow().Show();
        Close();
    }

    
    
    
    
    //управление
    private void OnPointerPressed(object sender, PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(JumpButton);
    
        if (point.Properties.IsLeftButtonPressed)
        {
            // ЛКМ - обычный прыжок
            Jump(+JumpForce);
        }
        else if (point.Properties.IsRightButtonPressed)
        {
            // ПКМ - вниз
            Jump(-JumpForce);
        }
        //отключение стандартного поведения
        e.Handled = true;
    }
    private void Jump(double force)
    {
        if (GoldSquare == null) return;
    
        var margin = GoldSquare.Margin;
        GoldSquare.Margin = new Thickness(
            margin.Left,
            margin.Top + force,
            margin.Right,
            margin.Bottom
        );
    }

    
    
    // Обработчик тика таймера
    private void Timer_Tick(object sender, EventArgs e)
    {
        Count++;
        TextCount.Text=Count.ToString();
    }
    
    

}