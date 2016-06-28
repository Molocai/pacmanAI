using System.Collections;

public class FSM {

	/// <summary>
    /// Déclaration du type delegate
	/// </summary>
	public delegate void state();

    /// <summary>
    /// Propriété qui portera le delegate
    /// </summary>
	private state activeState;

    /// <summary>
    /// Constructeur vide
    /// </summary>
	public FSM() {
	}

	/// <summary>
	/// Constructeur avec état initial
	/// </summary>
	/// <param name="startingState">L'état initial</param>
	public FSM(state startingState) {
		activeState = startingState;
	}

	/// <summary>
	/// Change l'état actuel pour un nouvel état
	/// </summary>
	/// <param name="newState">Le nouvel état</param>
	public void SetState(state newState) {
		activeState = newState;
	}

	/// <summary>
	/// Lance la FSM
	/// </summary>
	public void RunFSM() {
		if (activeState != null) {
			activeState();
		}
	}
}
