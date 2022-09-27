
import React from 'react';
import { Sidebar } from './sidebar';

class MainContent extends React.Component{
    constructor(props) {
        super(props);
        this.state = {
        };
      }
      render() {
                     
                return(<main className="col-md-9 ms-sm-auto col-lg-10 px-md-4">
                        
                        <form>
                        <fieldset>
                            <legend>Basic Info</legend>
                        
                            <div className="mb-3 row">
                                <label className="col-sm-2 col-form-label">Id</label>
                                <div className="col-sm-5">
                                <input type="text" readonly className="form-control-plaintext" value={this.props.content.id} />
                                </div>
                            </div>
                            <div className="mb-3 row">
                                <label className="col-sm-2 col-form-label">Name</label>
                                <div className="col-sm-5">
                                <input type="text" class="form-control" disabled readonly value={this.props.content.name}  />
                                </div>
                            </div>
                        </fieldset>
                        <fieldset>
                            <legend>Fields</legend>
                                {
                                    
                                    this.props.content.fields.map((field,index)=>{
                                                return (
                                                                <div className="mb-3 row">
                                                                    <label for={field.id} className="col-sm-2 col-form-label">{field.name}</label>
                                                                    <div className="col-sm-5">
                                                                    <input type="text" className="form-control" id={field.id} value={field.value}  />
                                                                    </div>
                                                                </div>
                                                )})
                                        
                                }
                        </fieldset>
                        </form>
                        
                        { JSON.stringify(this.props.content)}
                </main>);
                    }
            
}
export function BodyContainer(props){
    return (<div className="container-fluid">
                <div className="row">
                    <Sidebar links={props.links} />
                    <MainContent content={props.content} />
                </div>
    </div>);
}