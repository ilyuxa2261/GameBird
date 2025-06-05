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
    private DispatcherTimer _fallTimer; //отвечает за падение птички каждые 100мс
    private DispatcherTimer _obstacleTimer; //отвечает за генерацию новых столбов
    private DispatcherTimer _timer;//это для подсчета очков
    private const double Gravity = 10; //падение птички
    private const double JumpForce = -40; // подъем при клике
    private readonly Random _random = new(); //генератор случайных чисел
    private const int MinGap = 100; //минимальный размер дырки
    private const int MaxGap = 200; //максимальнок
    private const int ObstacleWidth = 60; //ширина столбов
    private const double ObstacleSpeed = 10; //скорость столбов
    private List<Border> _obstacles = new(); //список всех активных препятствий
    private int Count; //счетчик в таймере

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
        //таймер падения птички
        _fallTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        _fallTimer.Tick += OnFallTick!;
        _fallTimer.Start();
    }

    private void generate_obstacle()
    {
        //таймер генерации препятствий
        _obstacleTimer = new DispatcherTimer {};
        _obstacleTimer.Tick += SpawnObstacle!;
        _obstacleTimer.Start();
    }

    private void Raw()
    {
        // Подписываемся на Raw событие мыши, чтобы обойти стандартные обработчики Button
        AddHandler(PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
    }

    private void timer_count()
    {
        //обычный таймер
        Count = 0;
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += Timer_Tick;
        _timer.Start();
    }


    //движения и удаление за экраном
    private void OnFallTick(object sender, EventArgs e)
    {
        // гравитация для птички
        var margin = GoldSquare.Margin;
        GoldSquare.Margin = new Thickness(
            margin.Left,
            margin.Top + Gravity,
            margin.Right,
            margin.Bottom
        );
        
        //движение столбов
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
            
            // удаление за экраном
            if (obstacle.Margin.Left + ObstacleWidth < 0)
            {
                MainGrid.Children.Remove(obstacle);
                _obstacles.RemoveAt(i);
            }
        }
        
        CheckCollisions();
    }

    
    
    
    
    //генерация столбов
    private void SpawnObstacle(object sender, EventArgs e)
    {
        //в каком интервале времени появится новый столб
        //_obstacleTimer.Interval = TimeSpan.FromMilliseconds(_random.Next(3000, 8000));
        _obstacleTimer.Interval = TimeSpan.FromSeconds(_random.Next(3, 8));
        //позиция дырки может быть от самого верха до самого низа
        int gapY = _random.Next(0, (int)Height - MinGap);
        int gapHeight = _random.Next(MinGap, MaxGap);
    
        // Верхний столб (от верха экрана до верхнего края дырки)
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
    
        // Нижний столб (от нижнего края дырки до низа экрана)
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
        //добавляем в контейнер для отображения на экране
        MainGrid.Children.Add(topObstacle);
        MainGrid.Children.Add(bottomObstacle);
        //добавление в список активных препятствий
        _obstacles.Add(topObstacle);
        _obstacles.Add(bottomObstacle);
    }

    
    
    
    
    //проверка на столкновение
    private void CheckCollisions()
    {
        //проверяем позицию птички
        var birdRect = new Rect(
            GoldSquare.Margin.Left,
            GoldSquare.Margin.Top,
            GoldSquare.Width,
            GoldSquare.Height);
        //перебор всех препятствий
        foreach (var obstacle in _obstacles)
        {
            //создаю прямоугольник который представляет геометрическую область препятствия
            var obstacleRect = new Rect(
                obstacle.Margin.Left,
                obstacle.VerticalAlignment == VerticalAlignment.Top 
                    ? 0 
                    : Height - obstacle.Height,
                obstacle.Width,
                obstacle.Height);
            
            //проверка столкновения
            if (birdRect.Intersects(obstacleRect))
            {
                GameOver(Count);
                return;
            }
        }
        
        // проверка столкновения в границей экрана
        if (GoldSquare.Margin.Top < 0 || GoldSquare.Margin.Top + GoldSquare.Height > Height)
        {
            GameOver(Count);
        }
    }

    
    
    
    
    //все останавливаем, записываем в Json результат и пишем GameOver и потом перемещаемся в главное меню
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
        //если файл существует то берем данные из него, иначе создаем новую scoreData
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            scoreData = JsonSerializer.Deserialize<ScoreData>(json) ?? new ScoreData();
        }
        else
        {
            scoreData = new ScoreData();
        }
        
        //проверяю топ рейтинг, если он больше последнего значения то записываю в Top
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