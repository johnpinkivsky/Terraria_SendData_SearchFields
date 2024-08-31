#region Using

using Mono.Cecil;
using Mono.Cecil.Cil;

using MonoMod.Cil;

#endregion

// Пример: { Name: "controlJump" }, { FullName: "System.Boolean Terraria.Player::controlJump" }
// Так же стоит указывать именно поля класса "родительского". Т.е. если Terraria.NPC дочерний от Terraria.Entity
// и необходимо произвести поиск velocity, а он находится лишь в Terraria.Entity, то при вводе
// "Microsoft.Xna.Framework.Vector2 Terraria.NPC::velocity" произойдёт ошибка, необходимо ввести
// "Microsoft.Xna.Framework.Vector2 Terraria.Entity::velocity".

string FilePath = args.Length > 0 ? args[0] : "Terraria.exe";

AssemblyDefinition Game = AssemblyDefinition.ReadAssembly(FilePath);

TypeDefinition NetMessage = Game.MainModule.GetType("Terraria.NetMessage");
MethodDefinition SendData = NetMessage.Methods.First(m => m.Name == "SendData");

ILCursor cursor = new ILCursor(new ILContext(SendData));

cursor.GotoNext(i => i.OpCode.Code == Code.Switch);
Instruction Switch = cursor.Next;

Instruction[] targets = (cursor.Next.Operand as Instruction[])!;

Console.WriteLine("Введите поле, которое необходимо найти:");

string? field;
bool SearchField(Instruction i) 
    => i.Operand is FieldDefinition f && (f.Name == field || f.FullName == field);

while ((field = (field = Console.ReadLine())?.Trim()) != null)
{
    HashSet<int> switchTargets = new HashSet<int>();

    while (true)
    {
        try
        {
            cursor.GotoNext(SearchField);
        }
        catch (KeyNotFoundException) { break; }

        Instruction next = cursor.Instrs[cursor.Index + 2]; // cursor.Next.Next
        cursor.GotoPrev(i => targets.Contains(i));
        int index = Array.IndexOf(targets, cursor.Next) + 1;
        switchTargets.Add(index);
        cursor.Goto(next);
    }

    if (switchTargets.Count == 0)
        Console.WriteLine($"Не найдено поля с '{field}' названием.");
    else
        Console.WriteLine($"Поле '{field}' найдено в switch с (byte) значени{(switchTargets.Count > 1 ? "ями" : "ем")}: {string.Join(", ", switchTargets)}");

    cursor.Goto(Switch);
}

Console.WriteLine("Так как ничего не введено, поиск прекращён.");