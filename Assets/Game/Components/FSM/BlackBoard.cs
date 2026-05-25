using System;
using System.Collections.Generic;
using UnityEngine;

public class FieldBlackboard
{
    // ==========================================
    // 1. ДАННЫЕ ДЛЯ СВАПА (Записывает IdleState, читает SwapState)
    // ==========================================
    
    public SwapInfo SourceDest { get; set; }

    // ==========================================
    // 2. ДАННЫЕ ДЛЯ МАТЧЕЙ (Записывает MatchState, читает DestroyState)
    // ==========================================
    public List<MatchInfo> CurrentMatches { get; set; }

    // ==========================================
    // 2. ДАННЫЕ ДЛЯ БОНУСОВ (Записывает MatchState, читает FillUpState)
    // ==========================================
    public List<SpawnInfo> CurrentBonuses { get; internal set; }

    // ==========================================
    // 3. СОСТОЯНИЕ ТЕКУЩЕГО ХОДА И ИГРЫ
    // ==========================================
    // Номер каскада падений (0 - первый свап, 1 - первый обвал, 2 - комбо и т.д.)
    public int CascadeIteration { get; set; }

    // Флаг для проверки окончания игры (Game Over)
    public bool HasAvailableMoves { get; set; }

    // Данные для подсказки игроку
    //public HintInfo CurrentHint { get; set; }
    


    // ==========================================
    // МЕТОД СБРОСА (Вызывается при переходе в IdleState в конце хода)
    // ==========================================
    public void Reset()
    {

        // Очищаем список матчей, чтобы не держать ссылки в памяти
        CurrentMatches?.Clear();
        CurrentMatches = null;

        // Сбрасываем счетчик комбо
        CascadeIteration = 0;
    }
}