using UnityEngine;

public partial class NavalTestWorld
{
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
            if(NavalSessionSettings.PlayerShip==definition)return;
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
    private void ShipCard(float x,bool own)
    {
        const float y=14f,width=278f;
        GUI.Box(new Rect(x,y,width,283f),"");
        GUI.Label(new Rect(x+12f,y+7f,width-24f,23f),own?"СВОЙ КОРАБЛЬ":"ПРОТИВНИК",hudTitle);
        NavalHull ship=own?hull:enemy!=null?enemy.Hull:null;
        NavalStations controls=own?stations:enemy!=null?enemy.Controls:null;
        NavalShipDefinition definition=ship!=null?ship.Definition:own?NavalSessionSettings.PlayerShip:NavalSessionSettings.EnemyShip;
        float line=y+33f;
        Row(x+12f,line,"Тип",ShipName(definition));line+=18f;
        Row(x+12f,line,"Состояние",ship==null?"Не создан":ship.Sunk?"Тонет":"На плаву");line+=18f;
        if(ship!=null)
        {
            Row(x+12f,line,"Матросы",(own?(allies!=null?allies.LivingCrew:0):enemy.LivingCrew)+" / "+definition.CrewCount);line+=18f;
            Row(x+12f,line,"Пушки",controls.WorkingGuns+" / "+controls.GunCount);line+=18f;
            Row(x+12f,line,"Паруса",((own?rig.SailPowerFactor:enemy.SailPower)*100f).ToString("F0")+" %");line+=18f;
            Row(x+12f,line,"Скорость",(own?Mathf.Abs(speed):enemy.ShipVelocity.magnitude).ToString("F1")+" м/с");line+=18f;
            float roll=Mathf.DeltaAngle(0f,ship.Root.eulerAngles.z);
            Row(x+12f,line,"Крен",Mathf.Abs(roll).ToString("F1")+"° "+(roll>1f?"влево":roll< -1f?"вправо":""));line+=18f;
            Row(x+12f,line,"Вода",ship.Volume.ToString("F1")+" м³");line+=18f;
            Row(x+12f,line,"Лев. / прав.",ship.LeftWater.ToString("F0")+" / "+ship.RightWater.ToString("F0")+" м³");line+=18f;
            Row(x+12f,line,"Приток",ship.Inflow.ToString("F2")+" м³/с");line+=18f;
            Row(x+12f,line,"Повреждения",ship.HoleCount+" вокс.");
        }
        GUI.Label(new Rect(x+12f,y+234f,width-24f,18f),own?"Сменить свой — начать бой заново":"Заменить противника — 250 м",hudText);
        bool enabled=GUI.enabled;
        GUI.enabled=enabled&&ShipMenuOpen;
        foreach(int index in new int[]{0,1})
        {
            NavalShipDefinition choice=index==0?NavalShipDefinition.Sloop:NavalShipDefinition.Brigantine;
            GUI.enabled=enabled&&ShipMenuOpen&&(definition!=choice||!own&&ship==null);
            if(GUI.Button(new Rect(x+12f+index*130f,y+255f,124f,22f),ShipName(choice)))SelectProfile(own,choice);
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
        GUI.Label(new Rect(16f,300f,600f,22f),ShipMenuOpen?"ВЫБОР КОРАБЛЕЙ — F4 закрыть меню":"F4 — выбор кораблей  |  H — список клавиш",hudText);
        if(showHelp)
        {
            GUI.Box(new Rect(14f,327f,350f,416f),"");
            GUI.Label(new Rect(26f,334f,254f,22f),"УПРАВЛЕНИЕ",hudTitle);
            string[,] rows={
                {"F4","Выбор кораблей"},{"F6","Включить / выключить мод"},{"F7","Сильные / слабые волны"},
                {"F8","Вернуться на палубу"},{"F9","Пробоина слева"},{"F10 (держать)","Тестовая откачка"},
                {"F11","Пробоина справа"},{"F12","Убрать / создать врага"},{"E","Станция / дверь / лестница"},
                {"W A S D","Ходьба / управление"},{"ЛКМ / Space","Выстрел из пушки"},{"Space в воде","Подняться к поверхности"},
                {"Ctrl в воде","Погружаться"},{"G","Повредить выбранное"},{"R (держать)","Чинить выбранное"},
                {"Shift + R","Восстановить судно"},{"Q у помпы","Выбрать режим помпы"},{"T (держать)","Работать помпой"},{"H","Скрыть список клавиш"}
            };
            for(int i=0;i<rows.GetLength(0);i++)
            {
                GUI.Label(new Rect(26f,361f+i*19f,110f,19f),rows[i,0],hudText);
                GUI.Label(new Rect(136f,361f+i*19f,220f,19f),rows[i,1],hudText);
            }
        }
        float infoY=showHelp?750f:330f;
        string distance=enemy!=null?Vector3.Distance(boat.position,enemy.Boat.position).ToString("F0")+" м":"—";
        GUI.Label(new Rect(16f,infoY,650f,20f),"До противника: "+distance+"  |  "+(swimming?"Плавание":"На ногах")+"  |  Волны: "+(rough?"сильные":"слабые"),hudText);
        GUI.Label(new Rect(16f,infoY+20f,650f,20f),"v64  |  FPS: "+(1000f/Mathf.Max(1f,frameMilliseconds)).ToString("F0")+"  |  Вода CPU: "+ocean.BuildMilliseconds.ToString("F1")+" мс",hudText);
        GUI.matrix=previous;
    }
}
