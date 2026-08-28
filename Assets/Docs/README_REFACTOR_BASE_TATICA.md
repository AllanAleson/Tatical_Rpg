# Refatoracao da base tatica

Este documento resume as mudancas feitas para preparar o projeto para multiplas unidades jogaveis e inimigas, sem prender os sistemas a um unico Cube/player fixo.

## Scripts alterados

- `ClickManager`
- `Pathfinding`
- `PlayerMovement`
- `MovementHighlighter`
- `AttackHighLighter`
- `GridHover`
- `PathPreview`
- `MoveCostUI`
- `WorldHPText`
- `TurnManager`

## Selecao de unidades

- `ClickManager` agora usa `selectedUnit` como unidade ativa.
- `selectedUnit` e do tipo `PlayerMovement`.
- Clicar em uma unidade com `UnitStats.Team.Player` e `isDowned == false` seleciona essa unidade.
- O campo antigo `player` foi mantido como fallback de Inspector, mas a logica principal usa a unidade selecionada.
- Foram adicionados acessores:
  - `SelectedUnit`
  - `SelectedUnitStats`
- O fluxo aceita nenhuma unidade selecionada sem gerar `NullReferenceException`.
- Mensagens claras foram adicionadas para referencias importantes faltando.

## Pathfinding

- Removido o acoplamento com `public UnitStats unitStats` dentro de `Pathfinding`.
- `FindPath` agora recebe a unidade que esta se movendo:
  - `FindPath(Vector3 startPos, int targetX, int targetZ, UnitStats moverStats)`
- `CanReach` agora recebe a unidade que esta se movendo:
  - `CanReach(Vector3 startPos, int targetX, int targetZ, UnitStats moverStats)`
- O custo usa `moverStats.currentMovePoints`.
- `IsBlocked` usa:
  - `IsBlocked(Vector2Int cell, GameObject ignoredObject)`
- `IsBlocked`:
  - ignora `ignoredObject` e colliders filhos dele
  - bloqueia objetos com tag `Obstacle`
  - bloqueia qualquer objeto com `UnitStats` vivo
  - ignora unidades com `UnitStats.isDowned == true`
- A propria unidade que se move nao bloqueia o proprio pathfinding.
- Caminho para a propria celula retorna invalido.

## Movimento, preview e custo

- `MovementHighlighter`, `GridHover` e `PathPreview` agora usam a unidade selecionada no `ClickManager`.
- Movimento para a propria celula nao e permitido.
- Caminho com custo 0 nao e mostrado.
- `MoveCostUI` nao mostra `-0 PM`.
- Se o caminho custar mais PM do que a unidade tem, o preview e o custo sao escondidos.
- O preview continua usando `PathTilePrefab`.
- `MoveCostUI` mostra `-X PM` acima da celula de destino.
- `MoveCostUI` so aparece em `ActionMode.Move` com caminho valido e custo maior que 0.

## Ataque

- Ataque basico usa `selectedUnit`.
- Ataque nao acontece se a unidade selecionada estiver desmaiada.
- Ataque nao acontece sem PA suficiente.
- Ataque nao acontece fora do alcance.
- Ataque nao acerta unidade desmaiada.
- Friendly fire permanece possivel: o ataque basico nao bloqueia por `Team`.
- Depois de atacar, o alvo recebe dano e o atacante gasta PA.

## Highlights

- `MovementHighlighter` usa a unidade selecionada.
- `AttackHighlighter` usa a unidade selecionada.
- Ao trocar para `Move`:
  - limpa tiles de ataque
  - mostra alcance de movimento
- Ao trocar para `Attack`:
  - limpa tiles de movimento
  - mostra alcance de ataque
- Ao trocar para `Skill`:
  - limpa movimento
  - limpa ataque

## HP flutuante

- `WorldHPText` mostra:
  - `currentHP/maxHP` quando a unidade esta viva
  - `Desmaiado` quando `isDowned == true`
- O texto nao e destruido.
- O texto segue a unidade.
- O texto olha para a camera quando `Camera.main` existe.
- Logs de referencias faltantes foram protegidos para nao spammar todo frame.

## TurnManager

- `TurnManager` continua simples.
- `EndPlayerTurn` chama um turno inimigo placeholder e depois inicia novo turno do jogador.
- No novo turno do jogador, chama `ResetTurnPoints`.
- `ResetTurnPoints` ja ignora unidade desmaiada.
- Foi adicionado suporte opcional a `playerUnits` para restaurar multiplas unidades.
- Se `playerUnits` estiver vazio, o script usa `playerStats` como fallback.

## Validacao

- Nenhuma cena, prefab, material ou UI visual foi alterado.
- Foi executado:
  - `dotnet build Assembly-CSharp.csproj`
- Resultado:
  - compilacao com exito
  - 0 avisos
  - 0 erros

