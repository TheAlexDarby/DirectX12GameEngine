﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DirectX12GameEngine.Games;
using DirectX12GameEngine.Graphics;
using DirectX12GameEngine.Rendering;
using Microsoft.Extensions.DependencyInjection;

namespace DirectX12GameEngine.Engine
{
    public class Game : IDisposable
    {
        private readonly object tickLock = new object();

        private DateTime previousTime;
        private TimeSpan totalTime;

        public Game(GameContext gameContext)
        {
            GameContext = gameContext;

            PresentationParameters presentationParameters = new PresentationParameters(
                gameContext.RequestedWidth, gameContext.RequestedHeight, gameContext);

            switch (GameContext)
            {
                case GameContextHolographic context:
                    presentationParameters.Stereo = Windows.Graphics.Holographic.HolographicDisplay.GetDefault().IsStereo;
                    GraphicsDevice.Presenter = new HolographicGraphicsPresenter(GraphicsDevice, presentationParameters);
                    break;
                default:
                    GraphicsDevice.Presenter = new SwapChainGraphicsPresenter(GraphicsDevice, presentationParameters);
                    break;
            }

            GraphicsDevice.Presenter.ResizeViewport(
                GraphicsDevice.Presenter.PresentationParameters.BackBufferWidth,
                GraphicsDevice.Presenter.PresentationParameters.BackBufferHeight);

            Services = ConfigureServices();

            Content = Services.GetRequiredService<ContentManager>();
            SceneSystem = Services.GetRequiredService<SceneSystem>();
            GameSystems = Services.GetRequiredService<List<GameSystem>>();

            GameSystems.Add(SceneSystem);
        }

        public ContentManager Content { get; }

        public GameContext GameContext { get; }

        public IList<GameSystem> GameSystems { get; }

        public GraphicsDevice GraphicsDevice { get; } = new GraphicsDevice();

        public SceneSystem SceneSystem { get; }

        public IServiceProvider Services { get; }

        public GameTime Time { get; } = new GameTime();

        public virtual void Dispose()
        {
            GraphicsDevice.Dispose();

            foreach (GameSystem gameSystem in GameSystems)
            {
                gameSystem.Dispose();
            }
        }

        public void Run()
        {
            Initialize();
            LoadContentAsync();

            previousTime = DateTime.Now;

            switch (GameContext)
            {
                case GameContextXaml context:
                    Windows.UI.Xaml.Media.CompositionTarget.Rendering += (s, e) => Tick();
                    return;
#if NETCOREAPP
                case GameContextWinForms context:
                    System.Windows.Media.CompositionTarget.Rendering += (s, e) => Tick();
                    return;
#endif
            }

            Windows.UI.Core.CoreWindow? coreWindow = (GameContext as GameContextCoreWindow)?.Control;

            while (true)
            {
                coreWindow?.Dispatcher.ProcessEvents(Windows.UI.Core.CoreProcessEventsOption.ProcessAllIfPresent);
                Tick();
            }
        }

        public void Tick()
        {
            lock (tickLock)
            {
                DateTime currentTime = DateTime.Now;
                TimeSpan elapsedTime = currentTime - previousTime;

                previousTime = currentTime;
                totalTime += elapsedTime;

                Time.Update(totalTime, elapsedTime);

                Update(Time);

                BeginDraw();
                Draw(Time);
                EndDraw();
            }
        }

        protected void Initialize()
        {
            foreach (GameSystem gameSystem in GameSystems)
            {
                gameSystem.Initialize();
            }
        }

        protected virtual Task LoadContentAsync()
        {
            List<Task> loadingTasks = new List<Task>(GameSystems.Count);

            foreach (GameSystem gameSystem in GameSystems)
            {
                loadingTasks.Add(gameSystem.LoadContentAsync());
            }

            return Task.WhenAll(loadingTasks);
        }

        protected virtual void Update(GameTime gameTime)
        {
            foreach (GameSystem gameSystem in GameSystems)
            {
                gameSystem.Update(gameTime);
            }
        }

        protected virtual void BeginDraw()
        {
            GraphicsDevice.CommandList.Reset();

            if (GraphicsDevice.Presenter != null)
            {
                int width = GraphicsDevice.Presenter.PresentationParameters.BackBufferWidth;
                int height = GraphicsDevice.Presenter.PresentationParameters.BackBufferHeight;

                if (width != GraphicsDevice.Presenter.Viewport.Width || height != GraphicsDevice.Presenter.Viewport.Height)
                {
                    GraphicsDevice.Presenter.Resize(width, height);
                    GraphicsDevice.Presenter.ResizeViewport(width, height);
                }

                GraphicsDevice.Presenter.BeginDraw(GraphicsDevice.CommandList);

                GraphicsDevice.CommandList.SetViewport(GraphicsDevice.Presenter.Viewport);
                GraphicsDevice.CommandList.SetScissorRectangles(GraphicsDevice.Presenter.ScissorRect);
                GraphicsDevice.CommandList.SetRenderTargets(GraphicsDevice.Presenter.DepthStencilBuffer, GraphicsDevice.Presenter.BackBuffer);
            }

            foreach (GameSystem gameSystem in GameSystems)
            {
                gameSystem.BeginDraw();
            }
        }

        protected virtual void Draw(GameTime gameTime)
        {
            foreach (GameSystem gameSystem in GameSystems)
            {
                gameSystem.Draw(gameTime);
            }
        }

        protected virtual void EndDraw()
        {
            foreach (GameSystem gameSystem in GameSystems)
            {
                gameSystem.EndDraw();
            }

            GraphicsDevice.CommandList.Flush(true);

            GraphicsDevice.Presenter?.Present();
        }

        private IServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                .AddSingleton(this)
                .AddSingleton(GraphicsDevice)
                .AddSingleton<GltfModelLoader>()
                .AddSingleton<ContentManager>()
                .AddSingleton<List<GameSystem>>()
                .AddSingleton<SceneSystem>()
                .BuildServiceProvider();
        }
    }
}
