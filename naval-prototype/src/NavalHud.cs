using UnityEngine;

public partial class NavalTestWorld
{
            private static readonly string[,] spectatorHelp={
                {"F3","CPU-замеры"},{"W A S D","Свободный полёт"},{"ПКМ (держать)","Осматриваться"},{"Space / E","Подняться"},{"Ctrl / Q","Опуститься"},
                {"Shift","Лететь быстрее"},{"F8","Камера у своего корабля"},{"F4","Меню кораблей"},{"F7","Сильные / слабые волны"},{"F12","Убрать / создать врага"},{"H","Скрыть список клавиш"}
            };
            private static readonly string[,] liveHelp={
                {"F3","CPU-замеры"},{"F4","Выбор кораблей"},{"F6","Включить / выключить мод"},{"F7","Сильные / слабые волны"},
                {"F8","Вернуться на палубу"},{"F9","Пробоина слева"},{"F10 (держать)","Тестовая откачка"},
                {"F11","Пробоина справа"},{"F12","Убрать / создать врага"},{"E","Станция / лестница"},
                {"W A S D","Ходьба / управление"},{"ЛКМ / Space","Выстрел из пушки"},{"Space в воде","Подняться к поверхности"},
                {"Ctrl в воде","Погружаться"},{"G","Повредить выбранное"},{"R (держать)","Чинить выбранное"},
                {"Shift + R","Восстановить судно"},{"Q у помпы","Выбрать режим помпы"},{"T (держать)","Работать помпой"},{"H","Скрыть список клавиш"}
            };

    public bool RestartRequested {get;private set;}
    public bool ShipMenuOpen {get;private set;}
    private bool replaceEnemy,showHelp=true;
    private CursorLockMode oldCursorLock;
    private bool oldCursorVisible;
    private GUIStyle hudTitle,hudText;
    private void CloseShipMenu()
    {
        if(!ShipMenuOpen)return;
        ShipMenuOpen=false;Cursor.lockState=oldCursorLock;Cursor.visible=oldCursorVisible;
    }
    private void UpdateHudInput()
    {
        if(Input.GetKeyDown(KeyCode.F3))NavalPerformance.Toggle();
        if(Input.GetKeyDown(KeyCode.H))showHelp=!showHelp;
        if(Input.GetKeyDown(KeyCode.F4))
        {
            if(ShipMenuOpen)CloseShipMenu();
            else {oldCursorLock=Cursor.lockState;oldCursorVisible=Cursor.visible;ShipMenuOpen=true;}
        }
        if(ShipMenuOpen){Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
    }
    private string ShipName(NavalShipDefinition definition)
    {return definition==NavalShipDefinition.Sloop?"Шлюп":definition==NavalShipDefinition.Brigantine?"Бригантина":definition.Name;}
    private void SelectProfile(bool own,NavalShipDefinition definition)
    {
        if(own)
        {
            if(Spectating||NavalSessionSettings.PlayerShip==definition)return;
            NavalSessionSettings.PlayerShip=definition;CloseShipMenu();RestartRequested=true;
        }
        else
        {
            if(NavalSessionSettings.EnemyShip==definition&&enemy!=null)return;
            NavalSessionSettings.EnemyShip=definition;
            if(enemy!=null){Destroy(enemy.gameObject);enemy=null;}
            replaceEnemy=true;
        }
    }
    private void Row(float x,float y,string label,string value)
    {GUI.Label(new Rect(x,y,125f,19f),label,hudText);GUI.Label(new Rect(x+125f,y,140f,19f),value,hudText);}
    private readonly string[][] hudNumbers={new string[10],new string[10]};
    private readonly NavalHull[] hudNumberShips=new NavalHull[2];
    private readonly float[] hudNumberAt=new float[2];
    private readonly int[] hudNumberRevision={-1,-1};
    private string[] ShipNumbers(bool own,NavalHull ship,NavalStations controls,NavalShipDefinition definition)
    {
        int index=own?0:1;string[] values=hudNumbers[index];
        if(hudNumberShips[index]==ship&&hudNumberRevision[index]==ship.Revision&&Time.realtimeSinceStartup<hudNumberAt[index])return values;
        hudNumberShips[index]=ship;hudNumberRevision[index]=ship.Revision;hudNumberAt[index]=Time.realtimeSinceStartup+.2f;
        values[0]=(own?(allies!=null?allies.LivingCrew:0):enemy.LivingCrew)+" / "+definition.CrewCount;
        values[1]=controls.WorkingGuns+" / "+controls.GunCount;
        values[2]=((own?rig.SailPowerFactor:enemy.SailPower)*100f).ToString("F0")+" %";
        values[3]=(own?Mathf.Abs(speed):enemy.ShipVelocity.magnitude).ToString("F1")+" м/с";
        float roll=NavalHullStability.Roll(ship.Root);
        values[4]=Mathf.Abs(roll).ToString("F1")+"° "+(roll>1f?"влево":roll< -1f?"вправо":"");
        values[5]=ship.Volume.ToString("F1")+" м³";
        values[6]=ship.LeftWater.ToString("F0")+" / "+ship.RightWater.ToString("F0")+" м³";
        values[7]=ship.Inflow.ToString("F2")+" м³/с";
        values[8]=ship.HoleCount+" вокс.";
        float tankWater=0,tankCapacity=0;
        for(int i=0;i<4;i++)
        {
            NavalBallastTank tank=ship.Bilge!=null&&i<ship.Bilge.BallastCount?ship.Bilge.BallastAt(i):null;
            if(tank!=null){tankWater+=tank.Water;tankCapacity+=tank.Capacity;}
        }
        values[9]=tankWater.ToString("F1")+"/"+tankCapacity.ToString("F1")+" м³ · "+(tankCapacity>0?100f*tankWater/tankCapacity:0f).ToString("F0")+"%";
        return values;
    }
    private void ShipCard(float x,bool own)
    {
        const float y=14f,width=278f;
        GUI.Box(new Rect(x,y,width,301f),"");
        GUI.Label(new Rect(x+12f,y+7f,width-24f,23f),own?"СВОЙ КОРАБЛЬ":"ПРОТИВНИК",hudTitle);
        NavalHull ship=own?hull:enemy!=null?enemy.Hull:null;
        NavalStations controls=own?stations:enemy!=null?enemy.Controls:null;
        if(ship==null)hudNumberShips[own?0:1]=null;
        NavalShipDefinition definition=ship!=null?ship.Definition:own?NavalSessionSettings.PlayerShip:NavalSessionSettings.EnemyShip;
        float line=y+33f;
        Row(x+12f,line,"Тип",ShipName(definition));line+=18f;
        Row(x+12f,line,"Состояние",ship==null?"Не создан":ship.Sunk?"Тонет":(own?helmNavigator.Reversing:enemy.Reversing)?"Манёвр задним ходом":(own?retreat.Active:enemy.Retreating)?"Отход для ремонта":"На плаву");line+=18f;
        if(ship!=null)
        {
            string[] values=ShipNumbers(own,ship,controls,definition);
            Row(x+12f,line,"Матросы",values[0]);line+=18f;
            Row(x+12f,line,"Пушки",values[1]);line+=18f;
            Row(x+12f,line,"Паруса",values[2]);line+=18f;
            Row(x+12f,line,"Скорость",values[3]);line+=18f;
            Row(x+12f,line,"Крен",values[4]);line+=18f;
            Row(x+12f,line,"Вода",values[5]);line+=18f;
            Row(x+12f,line,"Лев. / прав.",values[6]);line+=18f;
            Row(x+12f,line,"Приток",values[7]);line+=18f;
            Row(x+12f,line,"Повреждения",values[8]);line+=18f;
            Row(x+12f,line,"Балласт",values[9]);
        }
        GUI.Label(new Rect(x+12f,y+252f,width-24f,18f),own?(Spectating?"Смена судна после перезапуска арены":"Сменить свой — начать бой заново"):"Заменить противника — 250 м",hudText);
        bool enabled=GUI.enabled;
        GUI.enabled=enabled&&ShipMenuOpen;
        for(int index=0;index<2;index++)
        {
            NavalShipDefinition choice=index==0?NavalShipDefinition.Sloop:NavalShipDefinition.Brigantine;
            GUI.enabled=enabled&&ShipMenuOpen&&!(own&&Spectating)&&(definition!=choice||!own&&ship==null);
            if(GUI.Button(new Rect(x+12f+index*130f,y+273f,124f,22f),ShipName(choice)))SelectProfile(own,choice);
        }
        GUI.enabled=enabled;
    }
    private void DrawStructuredHud()
    {
        if(hudTitle==null)
        {
            hudTitle=new GUIStyle(GUI.skin.label);hudTitle.fontSize=14;hudTitle.fontStyle=FontStyle.Bold;
            hudText=new GUIStyle(GUI.skin.label);hudText.fontSize=12;
        }
        Matrix4x4 previous=GUI.matrix;
        float scale=NavalHudLayout.Scale(Screen.width,Screen.height);
        GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,Vector3.one*scale);
        ShipCard(14f,true);ShipCard(302f,false);
        GUI.Label(new Rect(16f,318f,600f,22f),ShipMenuOpen?"ВЫБОР КОРАБЛЕЙ — F4 закрыть меню":"F4 — выбор кораблей  |  H — список клавиш",hudText);
        if(showHelp)
        {
            GUI.Box(new Rect(14f,345f,350f,416f),"");
            GUI.Label(new Rect(26f,352f,254f,22f),"УПРАВЛЕНИЕ",hudTitle);
            string[,] rows=Spectating?spectatorHelp:liveHelp;
            for(int i=0;i<rows.GetLength(0);i++)
            {
                GUI.Label(new Rect(26f,379f+i*19f,110f,19f),rows[i,0],hudText);
                GUI.Label(new Rect(136f,379f+i*19f,220f,19f),rows[i,1],hudText);
            }
        }
        float infoY=showHelp?768f:348f;
        string distance=enemy!=null?Vector3.Distance(boat.position,enemy.Boat.position).ToString("F0")+" м":"—";
        GUI.Label(new Rect(16f,infoY,650f,20f),"До противника: "+distance+"  |  "+(Spectating?"Наблюдатель":swimming?"Плавание":"На ногах")+"  |  Волны: "+(rough?"сильные":"слабые"),hudText);
        GUI.Label(new Rect(16f,infoY+20f,650f,20f),"v112  |  FPS: "+(1000f/Mathf.Max(1f,frameMilliseconds)).ToString("F0")+"  |  Вода CPU: "+ocean.BuildMilliseconds.ToString("F1")+" мс",hudText);
        GUI.matrix=previous;
        NavalPerformance.Draw();
    }
}
