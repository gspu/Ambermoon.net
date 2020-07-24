﻿/*
 * RenderMap3D.cs - Handles 3D map rendering
 *
 * Copyright (C) 2020  Robert Schneckenhaus <robert.schneckenhaus@web.de>
 *
 * This file is part of Ambermoon.net.
 *
 * Ambermoon.net is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * Ambermoon.net is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with Ambermoon.net. If not, see <http://www.gnu.org/licenses/>.
 */

using Ambermoon.Data;
using System.Collections.Generic;

namespace Ambermoon.Render
{
    internal class RenderMap3D : IRenderMap
    {
        public const int TextureWidth = 128;
        public const int TextureHeight = 80;
        public const int DistancePerTile = 3; // TODO
        public const int WallHeight = 2; // TODO
        readonly ICamera3D camera = null;
        readonly IMapManager mapManager = null;
        readonly IRenderView renderView = null;
        ITextureAtlas textureAtlas = null;
        ISurface3D floor = null;
        ISurface3D ceiling = null;
        Labdata labdata = null;
        readonly List<ISurface3D> walls = new List<ISurface3D>();
        readonly List<ISurface3D> overlays = new List<ISurface3D>();
        static readonly Dictionary<uint, ITextureAtlas> labdataTextures = new Dictionary<uint, ITextureAtlas>(); // contains all textures for a labdata (walls, objects and overlays)
        public Map Map { get; private set; } = null;

        public RenderMap3D(Map map, IMapManager mapManager, IRenderView renderView, uint playerX, uint playerY, CharacterDirection playerDirection)
        {
            camera = renderView.Camera3D;
            this.mapManager = mapManager;
            this.renderView = renderView;

            SetMap(map, playerX, playerY, playerDirection);
        }

        public void SetMap(Map map, uint playerX, uint playerY, CharacterDirection playerDirection)
        {
            if (map.Type != MapType.Map3D)
                throw new AmbermoonException(ExceptionScope.Application, "Tried to load a 2D map into a 3D render map.");

            camera.SetPosition(playerX * DistancePerTile, (map.Height - playerY) * DistancePerTile);
            camera.TurnTowards((float)playerDirection * 90.0f);

            if (Map != map)
            {
                CleanUp();

                Map = map;
                labdata = mapManager.GetLabdataForMap(map);
                EnsureLabdataTextureAtlas();
                UpdateSurfaces();
                // TODO: objects
            }
        }

        void CleanUp()
        {
            floor?.Delete();
            ceiling?.Delete();

            floor = null;
            ceiling = null;

            walls.ForEach(wall => wall?.Delete());
            overlays.ForEach(overlay => overlay?.Delete());

            walls.Clear();
            overlays.Clear();

            // TODO: objects
        }

        void EnsureLabdataTextureAtlas()
        {
            if (!labdataTextures.ContainsKey(Map.TilesetOrLabdataIndex))
            {
                var graphics = new Dictionary<uint, Graphic>();

                for (int i = 0; i < labdata.ObjectGraphics.Count; ++i)
                    graphics.Add((uint)i, labdata.ObjectGraphics[i]);
                for (int i = 0; i < labdata.WallGraphics.Count; ++i)
                    graphics.Add((uint)i + 1000u, labdata.WallGraphics[i]);

                labdataTextures.Add(Map.TilesetOrLabdataIndex, TextureAtlasManager.Instance.CreateFromGraphics(graphics, 1));
            }

            textureAtlas = labdataTextures[Map.TilesetOrLabdataIndex];
            renderView.GetLayer(Layer.Map3D).Texture = textureAtlas.Texture;
        }

        Position GetObjectTextureOffset(uint objectIndex)
        {
            return textureAtlas.GetOffset(objectIndex);
        }

        Position GetWallTextureOffset(uint wallIndex)
        {
            return textureAtlas.GetOffset(wallIndex + 1000u);
        }

        void AddWall(ISurface3DFactory surfaceFactory, IRenderLayer layer, uint mapX, uint mapY, uint wallIndex)
        {
            var wallTextureOffset = GetWallTextureOffset(wallIndex);

            void AddSurface(WallOrientation wallOrientation, float x, float z)
            {
                var wall = surfaceFactory.Create(SurfaceType.Wall, DistancePerTile, WallHeight, TextureWidth, TextureHeight, wallOrientation);
                wall.Layer = layer;
                wall.PaletteIndex = (byte)Map.PaletteIndex;
                wall.X = x;
                wall.Y = WallHeight;
                wall.Z = z;
                wall.TextureAtlasOffset = wallTextureOffset;
                wall.Visible = true; // TODO: not all walls should be always visible
                walls.Add(wall);
            }

            float baseX = mapX * DistancePerTile;
            float baseY = (-Map.Height + mapY) * DistancePerTile;

            // front face
            //if (mapY < Map.Height - 1 && Map.Blocks[mapX, mapY + 1].WallIndex == 0 && !Map.Blocks[mapX, mapY + 1].MapBorder)
                AddSurface(WallOrientation.Normal, baseX, baseY);

            // left face
            //if (mapX > 0 && Map.Blocks[mapX - 1, mapY].WallIndex == 0 && !Map.Blocks[mapX - 1, mapY].MapBorder)
                AddSurface(WallOrientation.Rotated90, baseX + DistancePerTile, baseY);

            // back face
            //if (mapY > 0 && Map.Blocks[mapX, mapY - 1].WallIndex == 0 && !Map.Blocks[mapX, mapY - 1].MapBorder)
                AddSurface(WallOrientation.Rotated180, baseX + DistancePerTile, baseY + DistancePerTile);

            // right face
            //if (mapX < Map.Width - 1 && Map.Blocks[mapX + 1, mapY].WallIndex == 0 && !Map.Blocks[mapX + 1, mapY].MapBorder)
                AddSurface(WallOrientation.Rotated270, baseX, baseY + DistancePerTile);
        }

        void UpdateSurfaces()
        {
            // Delete all surfaces
            floor?.Delete();
            ceiling?.Delete();
            walls.ForEach(s => s?.Delete());

            var surfaceFactory = renderView.Surface3DFactory;
            var layer = renderView.GetLayer(Layer.Map3D);

            // Add floor and ceiling
            floor = surfaceFactory.Create(SurfaceType.Floor, Map.Width * DistancePerTile, Map.Height * DistancePerTile, 64, 64); // TODO: texture size
            floor.PaletteIndex = (byte)Map.PaletteIndex;
            floor.Layer = layer;
            floor.X = 0.0f;
            floor.Y = 0.0f;
            floor.Z = -Map.Height * DistancePerTile;
            // Floors can have single colors or even tiled textures
            floor.TextureAtlasOffset = textureAtlas.GetOffset(1); // TODO: seems to have color rgb hex 11 22 33 in grandfathers cellar but couldn't find this color in the data
            floor.Visible = true;
            ceiling = surfaceFactory.Create(SurfaceType.Ceiling, Map.Width * DistancePerTile, Map.Height * DistancePerTile, 64, 64); // TODO: texture size
            ceiling.PaletteIndex = (byte)Map.PaletteIndex;
            ceiling.Layer = layer;
            ceiling.X = 0.0f;
            ceiling.Y = WallHeight;
            ceiling.Z = 0.0f;
            // Ceilings can have single colors, tiled textures or skyboxes
            ceiling.TextureAtlasOffset = textureAtlas.GetOffset(1); // TODO: seems to have color rgb hex 22 00 00 in grandfathers cellar but couldn't find this color in the data
            ceiling.Visible = true;

            // Add walls
            for (uint y = 0; y < Map.Height; ++y)
            {
                for (uint x = 0; x < Map.Width; ++x)
                {
                    var block = Map.Blocks[x, y];

                    if (block.WallIndex != 0)
                        AddWall(surfaceFactory, layer, x, y, block.WallIndex - 1);
                }
            }

            // TODO: add billboards (monsters, NPCs, flames, etc)
        }
    }
}
