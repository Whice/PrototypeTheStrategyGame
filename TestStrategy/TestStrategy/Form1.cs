using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
//using System.Linq;
using System.Text;
using System.Windows.Forms;
using Microsoft.DirectX.Direct3D;
using Microsoft.DirectX;
using LibraryForStrategy;

namespace TestStrategy
{
    public partial class WindowOfTheGame : Form
    {
        //Объявление устройств, буферов и т.д.
        Device device = null;
        private VertexBuffer vb = null;
        Texture[] allTextures = new Texture[100];
        Microsoft.DirectX.Direct3D.Font textForUnitInterface = null;
        string infoForDebag = "Все хорошо :Э";
        int countForDebag = 0;
        int MoveOfPlayer = 1;
        int maxPlayer = 2;
        string typeOfInterface = "";
        int tick = 0;
        int tickForInterfaceMoveToNextPlayer = 0;

        //Создание карты
        const int sizeOfLand = 100;
        CellOfLand[,] cellsOfLand = new CellOfLand[sizeOfLand, sizeOfLand];
        Point cursorPosition = new Point(0, 0);

        //Создание юнитов
        Unit[] units = new Unit[sizeOfLand * sizeOfLand];
        Point ActiveUnit = new Point(-1,-1);
        int[,] unitsID = new int[sizeOfLand , sizeOfLand];
        const int countUnitsOfGame = 3;//Количество существующих в игре(!не на карте!) юнитов
        Unit[] unitsForBuy = new Unit[countUnitsOfGame];//Перечисление существующих в игре(!не на карте!) юнитов
        bool[,] meshOfBuyUnitsCursor = new bool[10, 3];
        Point pointSpawnUnit = new Point();


        //Позиция и цель камеры
        private Vector3 myCameraPosition = new Vector3(0, 0, -12/*не надо больше это трогать*/);
        private Vector3 myCameraTarget = new Vector3(0, 0, 0);

        public WindowOfTheGame()
        {
            InitializeComponent();
            timer1.Start();
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.Opaque, true);
            this.Width = 700;
            this.Height = 700;

            //Создание текста
            //CreateAllText();

            //Инициализация юнитов и карты
            InitializeMapAndUnit();

            CreateMap();
            CreateUnitsForBuy();
            
            //Создание юнитов
            units[0].name = "Soldier";
            units[0].player = 1;
            units[0].team = 1;
            unitsID[0, 0] = 0;// id = порядок в массиве units
            units[0].HP = 100;

            units[2].name = "Timon";
            units[2].player = 1;
            units[2].team = 1;
            unitsID[2, 2] = 2;// id = порядок в массиве units
            units[2].HP = 100;

            units[1].name = "Gost";
            units[1].HP = 120;
            units[1].player = 2;
            units[1].team = 2;
            unitsID[4, 3] = 1;// id = порядок в массиве units
            units[1].HP = 100;

        }

        protected override void OnPaint(System.Windows.Forms.PaintEventArgs e)
        {
            device.Clear(ClearFlags.Target, 111, 1.0f, 0);

            
            device.BeginScene();
            device.RenderState.SourceBlend = Blend.SourceAlpha;
            device.RenderState.DestinationBlend = Blend.InvSourceAlpha;
            device.RenderState.AlphaBlendEnable = true;

            PaintMap();
            PaintUnits();
            PaintCursore();
            PaintUnitInterface1();
            PaintAllText();
            if (typeOfInterface != "")
            {
                PaintInterface();
            }
            
            SetupCamera();
            device.EndScene();

            this.Invalidate();

            device.Present();
            MyTimer();
        }

        private void SetupCamera()
        {
            device.Transform.Projection = Matrix.PerspectiveFovLH((float)Math.PI / 4, 1.7f, 1.0f, 15.0f);
            device.Transform.View = Matrix.LookAtLH(myCameraPosition, myCameraTarget, new Vector3(0, 1, 1));
            device.RenderState.Lighting = false;
            device.RenderState.CullMode = Cull.CounterClockwise;
        }

        public void InitializeGraphics()
        {
            // Set our presentation parameters
            PresentParameters presentParams = new PresentParameters();
            presentParams.Windowed = true;
            presentParams.SwapEffect = SwapEffect.Discard;
            // Create our device
            device = new Device(0, DeviceType.Hardware, this,
            CreateFlags.SoftwareVertexProcessing, presentParams);

            //Назначение тектур один раз. Сильно экономит оперативную память.
            CreateAllTextures();

            //Сильно экономит оперативную память.То же с текстом.
            CreateAllText();

            //Инициализация буфера vb один раз. Сильно экономит оперативную память.
            vb = new VertexBuffer(typeof(CustomVertex.PositionTextured), 4, device,
                Usage.Dynamic | Usage.WriteOnly, CustomVertex.PositionTextured.Format, Pool.Default);
            device.VertexFormat = CustomVertex.PositionTextured.Format;
        }

        private void PaintQuadro(Point controlPoint, /*int textureID*/ string textureAndName)
        {
            CustomVertex.PositionTextured[] verts = new CustomVertex.PositionTextured[4];
            verts[0] = new CustomVertex.PositionTextured(new Vector3(controlPoint.X, controlPoint.Y, 0.0f), 0, 0);
            verts[1] = new CustomVertex.PositionTextured(new Vector3(controlPoint.X, controlPoint.Y - 1, 0.0f), 0, 1);
            verts[2] = new CustomVertex.PositionTextured(new Vector3(controlPoint.X - 1, controlPoint.Y, 0.0f), 1, 0);
            verts[3] = new CustomVertex.PositionTextured(new Vector3(controlPoint.X - 1, controlPoint.Y - 1, 0.0f), 1, 1);
            
            
            vb.SetData(verts, 0, LockFlags.None);
            SetTextureOfGame(textureAndName);
            device.SetStreamSource(0, vb, 0);
            device.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);
        }

        private void PaintUnitInterface1()
        {
             for(int i=0;i<sizeOfLand; i++)
                 for (int j = 0; j < sizeOfLand; j++)
                 {
                     if (cursorPosition == new Point(i, j) && unitsID[i, j] > -1)
                         PaintUnitInteface2(units[unitsID[i,j]]);
                 }
        }

        private void PaintUnitInteface2(Unit unit)
        {
            Point interfaceWidthHeight = new Point(4, 6);
            Vector3 pointUnitInterface = new Vector3((float)(myCameraPosition.X + 10.6), (float)(myCameraPosition.Y - 0.3), myCameraPosition.Z+15);

            CustomVertex.PositionTextured[] verts = new CustomVertex.PositionTextured[4];
            verts[0] = new CustomVertex.PositionTextured(new Vector3(pointUnitInterface.X, pointUnitInterface.Y, pointUnitInterface.Z), 0, 0);
            verts[1] = new CustomVertex.PositionTextured(new Vector3(pointUnitInterface.X, pointUnitInterface.Y - interfaceWidthHeight.Y, pointUnitInterface.Z), 0, 1);
            verts[2] = new CustomVertex.PositionTextured(new Vector3(pointUnitInterface.X - interfaceWidthHeight.X, pointUnitInterface.Y, pointUnitInterface.Z), 1, 0);
            verts[3] = new CustomVertex.PositionTextured(new Vector3(pointUnitInterface.X - interfaceWidthHeight.X, pointUnitInterface.Y - interfaceWidthHeight.Y, pointUnitInterface.Z), 1, 1);


            vb.SetData(verts, 0, LockFlags.None);
            SetTextureOfGame("Svitok.png");
            device.SetStreamSource(0, vb, 0);
            device.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);

            
            string info = unit.name + ":\n\nHP   " +Convert.ToString(unit.HP)+"\nEXP   "+Convert.ToString(unit.EXP)+
             "\nLevel   "+Convert.ToString(unit.level)+"\nAtack   "+Convert.ToString(unit.atack)+"\nDefence   "+Convert.ToString(unit.defence);
            textForUnitInterface.DrawText(null, info, new Point(this.Width-220,this.Height-320), Color.BlueViolet);
        }

        private void PaintInterface()
        {
            if (typeOfInterface == "End turn?")
            {
                Point interfaceWidthHeight = new Point(8, 4);
                Vector3 pointInterface = new Vector3((float)(myCameraPosition.X + (interfaceWidthHeight.X / 2)), (float)(myCameraPosition.Y + (interfaceWidthHeight.Y / 2)), 0);

                CustomVertex.PositionTextured[] verts = new CustomVertex.PositionTextured[4];
                verts[0] = new CustomVertex.PositionTextured(new Vector3(pointInterface.X, pointInterface.Y, 0.0f), 0, 0);
                verts[1] = new CustomVertex.PositionTextured(new Vector3(pointInterface.X, pointInterface.Y - interfaceWidthHeight.Y, 0.0f), 0, 1);
                verts[2] = new CustomVertex.PositionTextured(new Vector3(pointInterface.X - interfaceWidthHeight.X, pointInterface.Y, 0.0f), 1, 0);
                verts[3] = new CustomVertex.PositionTextured(new Vector3(pointInterface.X - interfaceWidthHeight.X, pointInterface.Y - interfaceWidthHeight.Y, 0.0f), 1, 1);


                vb.SetData(verts, 0, LockFlags.None);
                SetTextureOfGame("Svitok.png");
                device.SetStreamSource(0, vb, 0);
                device.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);

                string info = "\n\t\tЗакончить ход?\n\n\n\tДа(Enter)\n\nНет(Escape)";
                textForUnitInterface.DrawText(null, info, new Point(this.Width/2-100, this.Height/2-100), Color.BlueViolet);
            }
            else if (typeOfInterface == "Move player")
            {
                Point interfaceWidthHeight = new Point(8, 4);
                Vector3 pointInterface = new Vector3((float)(myCameraPosition.X + (interfaceWidthHeight.X / 2)), (float)(myCameraPosition.Y + (interfaceWidthHeight.Y / 2)), 0);

                CustomVertex.PositionTextured[] verts = new CustomVertex.PositionTextured[4];
                verts[0] = new CustomVertex.PositionTextured(new Vector3(pointInterface.X, pointInterface.Y, 0.0f), 0, 0);
                verts[1] = new CustomVertex.PositionTextured(new Vector3(pointInterface.X, pointInterface.Y - interfaceWidthHeight.Y, 0.0f), 0, 1);
                verts[2] = new CustomVertex.PositionTextured(new Vector3(pointInterface.X - interfaceWidthHeight.X, pointInterface.Y, 0.0f), 1, 0);
                verts[3] = new CustomVertex.PositionTextured(new Vector3(pointInterface.X - interfaceWidthHeight.X, pointInterface.Y - interfaceWidthHeight.Y, 0.0f), 1, 1);


                vb.SetData(verts, 0, LockFlags.None);
                SetTextureOfGame("Svitok.png");
                device.SetStreamSource(0, vb, 0);
                device.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);

                string info = "Ход игрока " + Convert.ToString(MoveOfPlayer);
                textForUnitInterface.DrawText(null, info, new Point(this.Width / 2 - 100, this.Height / 2 - 100), Color.BlueViolet);

            }

            else if (typeOfInterface == "Create unit")
            {
                int x= Convert.ToInt32(myCameraPosition.X - 5);
                int y = Convert.ToInt32(myCameraPosition.Y+3);
                PaintAllCreateUnits(x, y);
                //Выделение описания
                Point interfaceWidthHeight = new Point(12, 3);
                Vector3 pointInterface = new Vector3((float)(myCameraPosition.X+6), (float)(myCameraPosition.Y), 0);
                Vector3 displacement = new Vector3(0,-0.4f,0);//Смещение описания

                CustomVertex.PositionTextured[] verts = new CustomVertex.PositionTextured[4];
                verts[0] = new CustomVertex.PositionTextured(new Vector3(
                    pointInterface.X,
                    pointInterface.Y + displacement.Y,
                    0.0f), 0, 0);
                verts[1] = new CustomVertex.PositionTextured(new Vector3(
                    pointInterface.X,
                    pointInterface.Y - interfaceWidthHeight.Y + displacement.Y,
                    0.0f), 0, 1);
                verts[2] = new CustomVertex.PositionTextured(new Vector3(
                    pointInterface.X - interfaceWidthHeight.X,
                    pointInterface.Y + displacement.Y,
                    0.0f), 1, 0);
                verts[3] = new CustomVertex.PositionTextured(new Vector3(
                    pointInterface.X - interfaceWidthHeight.X,
                    pointInterface.Y - interfaceWidthHeight.Y + displacement.Y,
                    0.0f), 1, 1);


                vb.SetData(verts, 0, LockFlags.None);
                SetTextureOfGame("Cursor.png");
                device.SetStreamSource(0, vb, 0);
                device.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);
            }


        }

        private void PaintAllCreateUnits(int x, int y)
        {
            Point interfaceWidthHeight = new Point(16, 10);
                Vector3 pointInterface = new Vector3((float)(myCameraPosition.X+8), (float)(myCameraPosition.Y+5), 0);

                CustomVertex.PositionTextured[] verts = new CustomVertex.PositionTextured[4];
                verts[0] = new CustomVertex.PositionTextured(new Vector3(pointInterface.X, pointInterface.Y, 0.0f), 0, 0);
                verts[1] = new CustomVertex.PositionTextured(new Vector3(pointInterface.X, pointInterface.Y - interfaceWidthHeight.Y, 0.0f), 0, 1);
                verts[2] = new CustomVertex.PositionTextured(new Vector3(pointInterface.X - interfaceWidthHeight.X, pointInterface.Y, 0.0f), 1, 0);
                verts[3] = new CustomVertex.PositionTextured(new Vector3(pointInterface.X - interfaceWidthHeight.X, pointInterface.Y - interfaceWidthHeight.Y, 0.0f), 1, 1);


                vb.SetData(verts, 0, LockFlags.None);
                SetTextureOfGame("Svitok.png");
                device.SetStreamSource(0, vb, 0);
                device.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);

                string info = "Покупка юнита:";
                string[,] specification = new string[10, 3];
                textForUnitInterface.DrawText(null, info, new Point(this.Width / 2 - 550, 40), Color.BlueViolet);
                PaintQuadro(new Point(x, y--), unitsForBuy[2].name);
                specification[0,2] = "Купить солдатика";
                PaintQuadro(new Point(x, y--), unitsForBuy[1].name);
                specification[0, 1] = "Купить грозного Тимона-бимона";
                PaintQuadro(new Point(x, y), unitsForBuy[0].name);
                specification[0, 0] = "Купить Каспера";
                

                for (int i = 0; i < 10; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        if (meshOfBuyUnitsCursor[i,j])
                        {
                            PaintQuadro(new Point(x+i,y+j), "Cursor.png");
                            textForUnitInterface.DrawText(null, specification[i,j], new Point(this.Width / 2 - 430, this.Height/2+40), Color.BlueViolet);
                        }
                    }
                }

        }

        private void PaintAllText()
        {
                textForUnitInterface.DrawText(null, infoForDebag, new Point(5, 5), Color.Azure);
        }

        private void CreateAllText()
        {
            //Unit interface text
            System.Drawing.Font drfont = new System.Drawing.Font("Mytext", 15f, FontStyle.Bold);
            textForUnitInterface = new Microsoft.DirectX.Direct3D.Font(device, drfont);
        }

        //Обработчик нажатия на клавиши клавиатуры
        private void WindowOfTheGame_KeyDown(object sender, KeyEventArgs e)
        {

            if (e.KeyCode == Keys.A)
            {
                myCameraPosition.X += 1;
                myCameraTarget.X += 1;
            }
            else if (e.KeyCode == Keys.D)
            {
                myCameraPosition.X -= 1;
                myCameraTarget.X -= 1;
            }
            else if (e.KeyCode == Keys.S)
            {
                myCameraPosition.Y += 1;
                myCameraTarget.Y += 1;
            }
            else if (e.KeyCode == Keys.W)
            {
                myCameraPosition.Y -= 1;
                myCameraTarget.Y -= 1;
            }
            else if (e.KeyCode == Keys.Escape && typeOfInterface == "")
            {
                this.Close();
            }
            else if (e.KeyCode == Keys.Up && cursorPosition.Y+1<sizeOfLand)
            {
                if (typeOfInterface == "Create unit")
                {
                    for (int i = 0; i < 10; i++)
                    {
                        for (int j = 0; j < 3; j++)
                        {
                            if (meshOfBuyUnitsCursor[i, j] && j+1<3)
                            {
                                meshOfBuyUnitsCursor[i, j] = false;
                                meshOfBuyUnitsCursor[i, j + 1] = true;
                                break;
                            }
                        }
                    }
                }
                else
                {
                    cursorPosition = new Point(cursorPosition.X, cursorPosition.Y + 1);
                    if (cursorPosition.Y > myCameraPosition.Y + 5)
                    {
                        myCameraPosition.Y++;
                        myCameraTarget.Y++;
                    }
                }

            }
            else if (e.KeyCode == Keys.Down && cursorPosition.Y - 1 >=0)
            {
                if (typeOfInterface == "Create unit")
                {
                    for (int i = 0; i < 10; i++)
                    {
                        for (int j = 0; j < 3; j++)
                        {
                            if (meshOfBuyUnitsCursor[i, j] && j-1>-1)
                            {
                                meshOfBuyUnitsCursor[i, j] = false;
                                meshOfBuyUnitsCursor[i, j -1] = true;
                            }
                        }
                    }
                }
                else
                {
                    cursorPosition = new Point(cursorPosition.X, cursorPosition.Y - 1);
                    if (cursorPosition.Y < myCameraPosition.Y - 4)
                    {
                        myCameraPosition.Y--;
                        myCameraTarget.Y--;
                    }
                }
            }
            else if (e.KeyCode == Keys.Left && cursorPosition.X -1 >=0)
            {
                if (typeOfInterface == "Create unit")
                {
                    for (int i = 0; i < 10; i++)
                    {
                        for (int j = 0; j < 3; j++)
                        {
                            if (meshOfBuyUnitsCursor[i, j] && i-1>-1)
                            {
                                meshOfBuyUnitsCursor[i, j] = false;
                                meshOfBuyUnitsCursor[i-1, j] = true;
                                break;
                            }
                        }
                    }
                }
                else
                {
                    cursorPosition = new Point(cursorPosition.X - 1, cursorPosition.Y);
                    if (cursorPosition.X < myCameraPosition.X - 7)
                    {
                        myCameraPosition.X--;
                        myCameraTarget.X--;
                    }
                }
            }
            else if (e.KeyCode == Keys.Right && cursorPosition.X + 1 < sizeOfLand)
            {
                if (typeOfInterface == "Create unit")
                {
                    for (int i = 0; i < 10; i++)
                    {
                        bool yes = false;
                        for (int j = 0; j < 3; j++)
                        {
                            if (meshOfBuyUnitsCursor[i, j] && i + 1 < 10)
                            {
                                meshOfBuyUnitsCursor[i, j] = false;
                                meshOfBuyUnitsCursor[i+1, j] = true;
                                yes = true;
                            }
                        }
                        if (yes) break;
                    }
                }
                else
                {
                    cursorPosition = new Point(cursorPosition.X + 1, cursorPosition.Y);
                    if (cursorPosition.X > myCameraPosition.X + 8)
                    {
                        myCameraPosition.X++;
                        myCameraTarget.X++;
                    }
                }
            }
            else if (e.KeyCode == Keys.Enter && typeOfInterface == "")
            {
                    ActionOnTheTownOrUnit();
            }
            else if (e.KeyCode == Keys.Enter && typeOfInterface == "Create unit")
            {
                CreateNewUnit();
                //Закрыть интерфейс покупки
                typeOfInterface = "";
            }
            else if (e.KeyCode == Keys.Space)
            {
                typeOfInterface = "End turn?";
            }
            else if (e.KeyCode == Keys.Escape && typeOfInterface == "End turn?")
            {
                typeOfInterface = "";
            }
            else if (e.KeyCode == Keys.Escape && typeOfInterface == "Create unit")
            {
                typeOfInterface = "";
            }
            else if (e.KeyCode == Keys.Enter && typeOfInterface == "End turn?")
            {
                MoveOfPlayer++;
                if (MoveOfPlayer > maxPlayer)
                    MoveOfPlayer = 1;
                tickForInterfaceMoveToNextPlayer = 30;
                typeOfInterface = "Move player";
            }
            else if (e.KeyCode == Keys.PageUp)
            {
                myCameraPosition.Z++;
            }
            else if (e.KeyCode == Keys.PageDown)
            {
                myCameraPosition.Z--;
            }
        }

        private void CreateAllTextures()
        {
            //Текстура cнега
            allTextures[0] =  TextureLoader.FromFile(device, @"..\..\" + "Snow.png");
            //Текстура горы
            allTextures[1] = TextureLoader.FromFile(device, @"..\..\" + "Mountain50X50.png");
            //Текстура леса
            allTextures[2] = TextureLoader.FromFile(device, @"..\..\" + "Forest50X50.png");
            //Курсор
            allTextures[3] = TextureLoader.FromFile(device, @"..\..\" + "Cursor.png");
            //Gost
            allTextures[98] = TextureLoader.FromFile(device, @"..\..\" + "Gost.jpg");
            //Timon
            allTextures[99] = TextureLoader.FromFile(device, @"..\..\" + "Timon.png");
            //Soldier
            allTextures[4] = TextureLoader.FromFile(device, @"..\..\" + "Soldier.png");
            //Interface
            allTextures[5] = TextureLoader.FromFile(device, @"..\..\" + "Svitok.png");
            //Castle
            allTextures[6] = TextureLoader.FromFile(device, @"..\..\" + "Castle100X100.png");
        }
        //Создание карты. Заполнение значениями.
        private void CreateMap()
        {
            for (int i = 0; i < sizeOfLand; i++)
                for (int j = 0; j < sizeOfLand; j++)
                {
                    if (i % 5 == 0)
                    {
                        cellsOfLand[i, j].name = "Mountain50X50.png";
                    }
                    else if (j % 3 == 0)
                    {
                        cellsOfLand[i, j].name = "Forest50X50.png";
                    }
                }

            cellsOfLand[1, 1].name = "Castle";
            cellsOfLand[1, 1].player = 1;
            cellsOfLand[5,5].name = "Castle";
            cellsOfLand[5,5].player = 2;
        }
        //Отрисовка карты
        private void PaintMap()
        {
            for (int i = 0; i < sizeOfLand; i++)
                for (int j = 0; j < sizeOfLand; j++)
                    PaintQuadro(new Point(i, j), cellsOfLand[i, j].name);
        }
        //Отрисовка курсора
        private void PaintCursore()
        {
              PaintQuadro(cursorPosition, "Cursor.png");
        }

        private void PaintUnits()
        {
            for(int i=0;i<sizeOfLand;i++)
                for(int j=0;j<sizeOfLand;j++)
                {
                    if(unitsID[i,j]>-1)
                    {
                        PaintQuadro(new Point(i, j), units[unitsID[i, j]].name);
                    }
                }
        }

        private void SetTextureOfGame(string texture)
        {
            if (texture == "Snow.png")
                device.SetTexture(0, allTextures[0]);
            else if (texture == "Forest50X50.png")
                device.SetTexture(0, allTextures[2]);
            else if (texture == "Mountain50X50.png")
                device.SetTexture(0, allTextures[1]);
            else if (texture == "Timon")
                device.SetTexture(0, allTextures[99]);
            else if (texture == "Gost")
                device.SetTexture(0, allTextures[98]);
            else if (texture == "Soldier")
                device.SetTexture(0, allTextures[4]);
            else if (texture == "Cursor.png")
                device.SetTexture(0, allTextures[3]);
            else if (texture == "Svitok.png")
                device.SetTexture(0, allTextures[5]);
            else if(texture == "Castle")
                device.SetTexture(0, allTextures[6]);
            else
                device.SetTexture(0, allTextures[0]);
        }

        private void ActionOnTheTownOrUnit()
        {

            for(int i=0;i<sizeOfLand; i++)
                for (int j = 0; j < sizeOfLand; j++)
                {
                    //Щелчок по юниту. Первый.
                    if (cursorPosition == new Point(i, j) && unitsID[i, j] > -1 && ActiveUnit == new Point(-1, -1)
                        && MoveOfPlayer == units[unitsID[i, j]].player)
                    {
                        if (units[unitsID[i, j]].active && ActiveUnit == new Point(i,j))
                        {
                            units[unitsID[i, j]].active = false;
                            ActiveUnit = new Point(-1, -1);
                        }
                        else if(ActiveUnit == new Point(-1,-1))
                        {
                            units[unitsID[i, j]].active = true;
                            ActiveUnit = new Point(i, j);
                        }
                    }
                    //перемещение юнита в пустое место
                    else if (cursorPosition == new Point(i, j) && unitsID[i, j] == -1 && ActiveUnit != new Point(-1,-1))
                    {
                        unitsID[i, j] = unitsID[ActiveUnit.X, ActiveUnit.Y];
                        unitsID[ActiveUnit.X, ActiveUnit.Y] = -1;
                        units[unitsID[i, j]].active = false;
                        ActiveUnit = new Point(-1, -1);
                    }
                    //замена выделенного юнита
                    else if (ActiveUnit != new Point(-1, -1) && units[unitsID[ActiveUnit.X, ActiveUnit.Y]].player == MoveOfPlayer && 
                        unitsID[i,j]>-1 && cursorPosition == new Point(i, j) && 
                        units[unitsID[i, j]].player == units[unitsID[ActiveUnit.X, ActiveUnit.Y]].player )
                    {
                        units[unitsID[ActiveUnit.X, ActiveUnit.Y]].active = false;
                        units[unitsID[i, j]].active = true;
                        ActiveUnit = new Point(i, j);
                    }
                    //атака активного юнита на указаный
                    else if (ActiveUnit != new Point(-1, -1) && cursorPosition == new Point(i, j)  &&
                        units[unitsID[i, j]].team != units[unitsID[ActiveUnit.X, ActiveUnit.Y]].team)
                    {
                        Atack(unitsID[ActiveUnit.X, ActiveUnit.Y], unitsID[i, j]);
                        units[unitsID[ActiveUnit.X, ActiveUnit.Y]].active = false;
                        ActiveUnit = new Point(-1, -1);
                    }
                    //Нажатие на замок
                    else if (cursorPosition == new Point(i, j) && ActiveUnit == new Point(-1, -1) && unitsID[i, j] == -1 &&
                        cellsOfLand[i,j].name == "Castle" && MoveOfPlayer == cellsOfLand[i,j].player)
                    {
                        AddNewUnitOnMap();
                    }

                }
            /*
            if (cursorPosition == Timon.position)
            {
                if (Timon.active == false)
                    Timon.active = true;
                else
                    Timon.active = false;
                if (ActiveUnit.X > -1)
                {

                }
            }
            else
            {
                if (Timon.active)
                {
                    Timon.position = cursorPosition;
                    Timon.active = false;
                }
            }*/
        }
        //Показывает интерфейс создания юнитов
        private void AddNewUnitOnMap()
        {
            pointSpawnUnit = cursorPosition;
            typeOfInterface = "Create unit";
            //infoForDebag = Convert.ToString(pointSpawnUnit.X) + " " + Convert.ToString(pointSpawnUnit.Y)+
            //            "\n" + Convert.ToString(unitsID[pointSpawnUnit.X, pointSpawnUnit.Y]);
            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    meshOfBuyUnitsCursor[i, j] = false;
                }
            }
            meshOfBuyUnitsCursor[0, 2] = true;
        }

        private void InitializeMapAndUnit()
        {
            //Инициализация карты
            for (int i = 0; i < sizeOfLand; i++)
                for (int j = 0; j < sizeOfLand; j++)
                {
                    cellsOfLand[i, j] = new CellOfLand();
                }
            //Инициализация юнитов
            for (int i = 0; i < sizeOfLand; i++)
                for (int j = 0; j < sizeOfLand; j++)
                {
                    unitsID[i, j] = new int();
                    unitsID[i, j] = -1;
                }

            for (int i = 0; i < sizeOfLand * sizeOfLand; i++)
            {
                units[i] = new Unit();
            }
        }
        private void Atack(int unitAtackerID, int unitDefenderID)
        {
            int uron = units[unitAtackerID].atack - units[unitDefenderID].defence;
            units[unitAtackerID].EXP += uron;
            units[unitDefenderID].HP -= uron;
            if (units[unitDefenderID].HP < 1)//Юнит будет мертв и не будет рисоваться
            {
                units[unitDefenderID].HP = 0;
                for(int i=0;i<sizeOfLand;i++)
                    for (int j = 0; j < sizeOfLand; j++)
                    {
                        if (unitsID[i, j] == unitDefenderID)
                            unitsID[i, j] = -1;
                    }
            }
        }

        private void MyTimer()
        {
            tick++;
            if (tickForInterfaceMoveToNextPlayer > 0)
            {
                tickForInterfaceMoveToNextPlayer--;
            }
            else if(typeOfInterface == "Move player")
            {
                typeOfInterface = "";
            }
        }

        private void CreateUnitsForBuy()
        {
            for (int i = 0; i < countUnitsOfGame; i++)
            {
                unitsForBuy[i] = new Unit();
            }
            //Создание солдата
            unitsForBuy[1].name = "Soldier";
            //Timon
            unitsForBuy[1].name = "Timon";
            //Gost
            unitsForBuy[0].name = "Gost";
        }
        //Создает нового юнита, указанного в интерфейсе
        private void CreateNewUnit()
        {
            //Ищу какой юнит был выбран
            Point newUnit = new Point();
            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (meshOfBuyUnitsCursor[i, j])
                    {
                        newUnit = new Point(i,j);
                    }
                }
            }
            //Ищу свободное место в массиве существующих юнитов
            for (int i = 0; i < sizeOfLand * sizeOfLand; i++)
            {
                if (units[i].HP == 0)
                {
                    units[i].name = unitsForBuy[newUnit.X+newUnit.Y].name;
                    units[i].player = MoveOfPlayer;
                    units[i].team = 1;
                    if(unitsID[pointSpawnUnit.X, pointSpawnUnit.Y] == -1)//если место свободно
                        unitsID[pointSpawnUnit.X, pointSpawnUnit.Y] = i;// id = порядок в массиве units
                    units[i].HP = 100;
                    break;
                }
            }
        }
    }
}